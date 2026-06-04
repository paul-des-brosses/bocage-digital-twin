# ARCHITECTURE.md — Architecture technique

Document technique d'architecture du Bocage Digital Twin. À lire en
complément de `CLAUDE.md` (règles opérationnelles) et `DECISIONS.md`
(rationale des choix).

> **Mis à jour 2026-06-04** : réconciliation avec le code post-E8/E9
> (refonte delta tech + système de recommandations).

---

## 1. Schéma général en 5 couches

```mermaid
graph TB
    subgraph L5["05 — Presentation (Unity)"]
        UI[Dashboard UI]
        Scene[Scene Renderer]
        Bindings[Bindings]
        Runner[Simulation Runner]
        Shadow[Shadow Simulation Runner]
        AutoApply[Auto Action Applier]
    end

    subgraph L4["04 — Indicators"]
        Hero[Hero Indicators (per-KPI classes)]
        LevelB[Level B Panels]
    end

    subgraph L3["03 — Decision"]
        RecEngine[Recommendation Engine]
        Outcome[Outcome Projector]
        AutoActions[Auto Actions]
        Journal[Decision Journal]
    end

    subgraph L2["02 — Sensors"]
        Sensors[Sensor Implementations]
        EventDetect[Event Detector]
        History[Measurement History]
    end

    subgraph L1["01 — Simulation Core (pure C#)"]
        Engine[Simulation Engine]
        Model[Ecosystem Model]
        Rules[Biophysical Rules]
        Random[Seeded Random]
        Context[Scenario Context]
    end

    Sensors --> Model
    EventDetect --> Sensors
    History --> Sensors

    RecEngine --> EventDetect
    Outcome --> Model
    AutoActions --> RecEngine
    Journal --> RecEngine

    Hero --> Model
    Hero --> Sensors
    LevelB --> Model
    LevelB --> Sensors

    Runner --> Engine
    Runner --> Hero
    Runner --> RecEngine
    Shadow --> Engine
    AutoApply --> AutoActions
    Bindings --> Hero
    Bindings --> LevelB
    Bindings --> Journal
    UI --> Bindings
    Scene --> Bindings
    Runner --> Context
    UI --> RecEngine
```

**Lecture** : une flèche `A --> B` signifie « A référence / dépend de B ».
Les flèches descendent toujours vers une couche d'indice strictement
inférieur. Aucune flèche ne remonte.

---

## 2. Description par couche

### Couche 01 — SimulationCore

**Responsabilité**

Modélisation biophysique pure du bocage. Tient l'état complet de
l'écosystème simulé et applique les règles de dynamique à chaque tick.

**Composants principaux**

- `SimulationEngine` : orchestrateur de tick, gère le temps simulé et la
  séquence d'application des règles.
- `EcosystemModel` : conteneur d'état (haies, prairie, bosquet, mare,
  arbres têtards, faune agrégée, météo, nappe, sol, etc.).
- `BiophysicalRules` : règles de dynamique (croissance, dégradation,
  hydrologie, propagation pathogènes, etc.). Implémente `IRule`.
- `SeededRandom` : générateur d'aléa déterministe avec sous-seeds dérivés
  par hash.
- `ScenarioContext` : paramètres scénario en cours (climat, pression
  agricole, contraintes réglementaires, horizon).
- `TransitioningParameter<T>` : interpolation des paramètres de scénario
  sur 7-14 jours simulés.

**Non-responsabilités**

- Aucun accès Unity (`UnityEngine`, `UnityEditor` interdits).
- Aucun I/O fichier ou réseau.
- Aucun rendu, aucun affichage.
- Aucune dépendance vers les couches supérieures.

**Dépendances** : aucune, hormis BCL .NET Standard 2.1.

---

### Couche 02 — Sensors

**Responsabilité**

Transforme l'état du modèle en mesures bruitées (modèle de capteur
réaliste) et détecte les événements significatifs.

**Composants principaux**

- `ISensor` : interface commune.
- Implémentations dans `Implementations/` (station météo, piézomètre,
  capteur acoustique, piège photo, tour eddy covariance).
- `FaunaSensorReader` : synthèse Gaussienne combinant deux capteurs
  indépendants (acoustique + piège photo) avec σ ∝ 1/√fauna (théorie
  Poisson : abondances rares → estimations plus bruitées). Sous-flux
  RNG dédié `"fauna-sensors"` dérivé du seed maître pour la
  reproductibilité indépendante des autres sous-systèmes. Sa lecture
  alimente l'`EventDetector` pour l'alerte fauna.
- `EventDetector` : compare l'état du modèle (et la lecture mesurée
  fauna, plus les indicateurs économiques fournis par la Couche 04) à
  des seuils calibrés pour émettre des événements (sécheresse prolongée,
  anomalie acoustique, carbone sol bas, rentabilité anormalement basse).
  Seuils : carbone < 45 tC/ha, rentabilité < 50 €/ha. Cooldown par type
  pour éviter le spam.
- `Events/` : `IEvent` + classes d'événements concrets
  (`DroughtProlongedEvent`, `FaunaAcousticAnomalyEvent`,
  `SoilCarbonLowEvent`, `LowProfitabilityEvent`).
- `EventLog` : append-only chronologique des événements émis.

**Non-responsabilités**

- Ne modifie jamais l'état du modèle.
- Ne prend aucune décision.
- Ne touche pas à l'UI.

**Dépendances** : Couche 01 uniquement.

---

### Couche 03 — Decision

**Responsabilité**

Génère des recommandations à partir des événements détectés, projette
des issues probables sous incertitude, journalise les décisions.

**Composants principaux**

- `RecommendationEngine` : produit au plus une recommandation par
  événement non encore traité, en choisissant le levier qui rapproche le
  bocage de l'équilibre DEPUIS L'ÉTAT COURANT (dispatch *state-aware*,
  chantier E9). Sécheresse → irrigation ; anomalie fauna → le levier
  habitat/intrants disposant de marge, du plus rapide au plus coûteux
  (baisse d'intrants → arrêt de l'arrachage → plantation, sinon silence) ;
  carbone bas → couverts puis restitution résidus ; rentabilité basse →
  contre-recommandations économiques (remonter intrants, éclaircir haies),
  jamais au détriment d'une biodiversité déjà critique.
- `Recommendations/` : interface `IRecommendation` + 8 classes concrètes
  réparties sur 6 leviers : `PlantHedgesRecommendation`,
  `IrrigationAdviceRecommendation`, `ReduceInputsRecommendation`,
  `RaiseInputsRecommendation`, `SowCoverCropsRecommendation`,
  `RestoreResidueRecommendation`, `ReduceHedgeRemovalRecommendation`,
  `IncreaseHedgeRemovalRecommendation`. `PlantHedgesRecommendation` est
  désormais produite par le moteur (réponse habitat à une anomalie fauna,
  une fois le levier intrants épuisé) ET déclenchable manuellement.
- `OutcomeProjector` : projette des distributions d'issues à 2 horizons
  (30 j et 365 j) sous forme de 3-points (worst / expected / best) — pas
  de Monte-Carlo, distributions calibrées en dur (cf `DECISIONS.md`).
- `RecommendationProvenance` : helper de formatage pur (pas de Unity)
  qui résout l'`IEvent` source d'une reco depuis l'`EventLog` et
  renvoie une ligne « Détecté jour N par <capteur> — <event summary> »
  consommée par le popup décision et la liste historique (sub-étape 10a).
- `AutoActionPipeline` : applique l'effet mécanique de chaque reco
  Accepted / AutoAccepted, sur le run réel uniquement (jamais le shadow).
  Idempotent via `DecisionJournal.MarkApplied/IsApplied`. Deux familles
  de leviers : les actions de capital (plantation de haies, irrigation)
  mutent directement l'`EcosystemModel` (`SetHedgerowDensity`,
  `SetWaterTableDepth`) ; les changements de pratique quotidienne
  (intensité d'intrants, couverts d'interculture, restitution résidus,
  taux d'arrachage) déplacent le slider correspondant du `ScenarioContext`
  sur une transition de 10 jours simulés (`PracticeTransitionDays`,
  CLAUDE.md §15). La baisse d'intrants n'est plus un *nudge* ponctuel du
  modèle (l'ancienne triche ADR #43, retirée en E8) mais un déplacement
  durable du slider `InputIntensityFactor`, planché à l'extensif bio.
- `RecommendationSurfacing` : classe chaque reco selon le SIGNE de son
  issue projetée (`OutcomeProjector`) dans `Kind { WinWin,
  EconomicTradeoff, EcologicalTradeoff, LoseLose }` et décide si elle
  interrompt l'utilisateur (popup) ou reste en liste passive. Le twin
  n'interrompt que pour les gains sans perdant et les urgences
  écologiques (un correctif écologique coûteux escalade en popup quand la
  biodiversité est critique) ; tout compromis confort ou économie-contre-
  écologie reste en liste passive avec un marqueur « compromis ». Pure
  Couche 03, testable en EditMode.
- `DecisionJournal` : journal append-only des décisions (utilisateur ou
  algorithmiques) avec horodatage simulé. Verdicts : `Pending`,
  `Accepted`, `Rejected`, `AutoAccepted`, et `Superseded` (auto-marqué
  quand un nouveau Pending de même type arrive — au plus 1 Pending par
  type à un instant donné, cf ADR #44).
- `DecisionVerdict` : enum des verdicts ci-dessus.

**Non-responsabilités**

- Ne lit pas l'UI directement (l'UI consomme les recommandations via
  observable).
- Ne mute le modèle qu'au travers de l'`AutoActionPipeline`, et seulement
  pour les recos dont le verdict est Accepted / AutoAccepted : les actions
  de capital mutent directement l'`EcosystemModel` via ses méthodes
  `Set*`, les actions de pratique déplacent les sliders du
  `ScenarioContext`. Aucune interface `IModelAction` n'existe.

**Dépendances** : Couches 01 et 02.

---

### Couche 04 — Indicators

**Responsabilité**

Agrège l'état du modèle et les mesures en KPIs et panneaux. Fournit les
classes d'indicateurs par KPI consommées par la Couche 05.

**Composants principaux**

Indicateurs purs (une classe par KPI, fonctions de calcul + normalisation
pour la jauge), dossier `Hero/` :

- `HedgerowDensityIndicator`, `BiodiversityCompositeIndicator`,
  `WaterTableIndicator`, `IntegratedProfitabilityIndicator`,
  `SoilCarbonIndicator` — les 5 Hero KPIs « état » (densité haies,
  biodiversité composite, nappe phréatique, rentabilité intégrée, carbone
  sol), plus les indicateurs dérivés `SoilMoistureIndicator` et
  `HedgerowHealthIndicator` qui alimentent les canaux shaders.
- `CumulativeTechValueIndicator` : intégrale courante en €/ha de
  l'avantage de rentabilité du run réel sur le run shadow depuis le jour
  0 (part GROSSE seulement ; le KPI affiché est le NET, cf §6). Stateful
  par nature.
- `InvestmentHorizonIndicator` : latch du « horizon de rentabilité » —
  premier jour simulé où le NET (intégrale tech moins investissement
  cumulé `DecisionJournal.TotalInvestmentEurosPerHectare`) atteint le
  point mort (NET ≥ 0), à condition qu'un investissement existe à
  amortir. N'a plus d'intégrale propre (refonte E8) : on lui passe le NET
  même que le Hero KPI affiche.

Note : il n'y a pas de classes `HeroIndicators` / `LevelBPanels`
agrégatrices ; les panneaux Niveau B sont assemblés côté Couche 05 par
les bindings d'onglets (`OngletBiodiv/Climat/EconomieBinding`) qui lisent
ces indicateurs. Le `ShadowSimulationRunner` n'est PAS dans cette couche
mais dans la Couche 05 (cf ci-dessous et §6). Un reporter de session est
un item *backlog* (#4), non implémenté.

**Non-responsabilités**

- Ne mute jamais le modèle.
- Ne décide pas (consomme les sorties de la Couche 03).
- N'exécute pas elle-même la simulation fantôme (orchestrée en Couche 05).

**Dépendances** : Couches 01, 02 et 03.

---

### Couche 05 — Presentation

**Responsabilité**

MonoBehaviours Unity. Rendu de la scène, UI, bindings vers les
ScriptableObjects observables, gestion des inputs utilisateur.

**Composants principaux**

- `DashboardUI` : Hero KPIs, panneaux Niveau B, popovers Niveau C,
  scenario panel, decision panel, comparison view, minimap.
- `SceneRenderer` : organisation des sprites de scène (background,
  midground, foreground, fauna, sensors), shaders pilotés par
  observables.
- `Bindings` : MonoBehaviours qui écoutent les ScriptableObjects
  observables (`OnChanged`) et mettent à jour les éléments visuels et UI.
- `SimulationRunner` : orchestrateur du run réel. Possède l'unique
  `SimulationEngine` réel et le cadence via une coroutine
  (`WaitForSecondsRealtime`, indépendante de `Time.timeScale`). À chaque
  tick : lit les capteurs, lance détection + recommandations, déclenche
  les souscripteurs (`TickCompleted`), met à jour les accumulateurs
  (tech-value GROSSE + latch horizon) puis publie les indicateurs dans
  les conteneurs `RC_*`. Unique écrivain de ces conteneurs ; les bindings
  ne font que lire.
- Les classes `*Binding` de scénario (`ScenarioControlsBinding`,
  `InitialConditionsBinding`, `MonthSelectorBinding`,
  `ManualActionsBinding`, etc.) capturent les inputs utilisateur (sliders,
  clics, vitesses) et les répercutent dans le `ScenarioContext` ou via les
  méthodes `ApplyManual*` du `SimulationRunner`. Il n'existe pas de classe
  `InputManager`.
- `AutoActionApplier` : wrapper MonoBehaviour autour de
  l'`AutoActionPipeline`. Souscrit à `SimulationRunner.TickCompleted` et
  applique sur le run réel les recos Accepted / AutoAccepted (le shadow
  n'est jamais touché).
- `ShadowSimulationRunner` : run fantôme « agriculteur passif »
  (cf §6). Vit en Couche 05 car il dépend des MonoBehaviours Unity
  (`SimulationRunner`), pas en Couche 04.

**Non-responsabilités**

- Ne contient aucune logique biophysique.
- Ne calcule aucun KPI directement.
- Ne mute pas l'état du modèle de simulation.

**Dépendances** : toutes les couches inférieures.

---

## 3. Flux principal de données

À chaque tick :

1. **Inputs utilisateur** captés par les bindings de scénario de la
   Couche 05 → mis à jour dans le `ScenarioContext` (avec interpolation
   via `TransitioningParameter<T>`).
2. **Tick de simulation** : `SimulationEngine.Tick()` applique les règles
   biophysiques sur l'`EcosystemModel`, en utilisant `SeededRandom` et
   le `ScenarioContext`.
3. **Lecture capteurs** : chaque `ISensor` lit l'état du modèle et
   produit une mesure bruitée. `EventDetector` examine les mesures (plus
   les indicateurs économiques fournis par la Couche 04) et ajoute les
   événements détectés à l'`EventLog` (append-only) — pas d'EventBus.
4. **Décision** : `RecommendationEngine` consomme les événements de
   l'`EventLog` et produit / met à jour les recommandations dans le
   `DecisionJournal`. L'`AutoActionApplier` applique sur le run RÉEL les
   recos Accepted / AutoAccepted (idempotence via
   `DecisionJournal.IsApplied`) ; le shadow n'est jamais touché.
5. **Indicateurs** : les indicateurs par KPI (Couche 04) recalculent les
   valeurs à partir de l'état + mesures. Le `ShadowSimulationRunner` a
   déjà avancé son propre modèle au même tick ; le `SimulationRunner`
   intègre l'écart de rentabilité réel − shadow du jour. Le delta est
   calculé (cf §6).
6. **Bindings** : les composants `Bindings` écrivent les valeurs dans
   les ScriptableObjects observables, qui notifient leurs abonnés via
   `OnChanged`.
7. **UI et Scene** : les abonnés (UI widgets, shaders pilotés) lisent
   les nouvelles valeurs et se rafraîchissent.

Ce flux est descendant à l'aller (input → modèle → indicateurs) et
remontant au retour (observables → UI). À aucun moment une couche
inférieure ne lit une couche supérieure.

---

## 4. Cycle de vie d'une session utilisateur

1. **Bootstrap** : chargement de la scène `Main`. Le `SimulationRunner`
   construit le `SimulationEngine` réel ; le `ShadowSimulationRunner`
   construit le run fantôme (même seed maître, scénario gelé dérivé via
   `CreateFrozenShadowFrom`). Le `ScenarioContext`, les ScriptableObjects
   observables et les bindings sont initialisés.
2. **État initial affiché** : KPIs initiaux, scène statique avec sprites
   en place, recommandations vides, journal vide.
3. **Lecture utilisateur** : l'utilisateur observe l'état initial,
   éventuellement règle un preset via le scenario panel.
4. **Lancement de la simulation** : utilisateur appuie play. Tick rate
   x1 par défaut.
5. **Boucle de simulation** : tick après tick, les KPIs évoluent, des
   événements peuvent être détectés et apparaissent dans le decision
   panel sous forme de recommandations.
6. **Arbitrage utilisateur** : l'utilisateur accepte ou rejette les
   recommandations. Les choix sont journalisés.
7. **Modification de scénario en cours** : changement de preset →
   transition interpolée 7-14 jours simulés.
8. **Skip to end** : l'utilisateur peut sauter à l'horizon configuré.
9. **État final** : les KPIs finaux restent affichés sur le dashboard ;
   l'apport de la techno (NET) et l'horizon de rentabilité sont lisibles
   en direct. Un rapport de session synthétique est un item *backlog*
   (#4), non implémenté à ce stade.
10. **Persistance** : `PlayerPrefs` sauvegarde uniquement la dernière
    configuration de presets et la vitesse choisie.

---

## 5. Modèle d'horloges

Trois horloges distinctes :

- **Temps réel** (`Time.unscaledDeltaTime` Unity) : utilisé uniquement
  pour les animations cosmétiques de la Couche 5 (interpolations
  visuelles, transitions UI).
- **Temps simulé** : avance d'un tick = un jour simulé. Cadencé par le
  `SimulationRunner` (Couche 05) qui appelle `SimulationEngine.Tick()`
  via une coroutine indépendante de `Time.timeScale` (la Couche 01 est du
  C# pur, sans chrono).
- **Vitesse utilisateur** : x1 (1 tick / seconde temps réel), x10 (10
  ticks / seconde), skip to end (boucle exécutée au plus vite jusqu'à
  l'horizon).

Comportement sur **pause** : le temps simulé est gelé, les animations
Couche 5 continuent à s'exécuter (les sprites de faune en pool restent
animés visuellement). C'est un choix délibéré pour éviter une scène
figée disgracieuse.

Le **skip to end** désactive temporairement les bindings vers la Couche
5 (pas de rafraîchissement intermédiaire de l'UI), exécute les ticks au
plus vite, puis pousse une mise à jour finale.

---

## 6. Gestion de la simulation fantôme

Il n'y a pas d'interface `ISimulationRun` ni de drapeau
`applyTechActions`. Deux `SimulationEngine` indépendants sont construits à
partir du **même seed maître**, chacun avec son propre `EcosystemModel` :

- **Real run** : possédé par le `SimulationRunner` (Couche 05). Les
  décisions de l'agriculteur (déplacements de sliders et actions
  appliquées) le font évoluer.
- **Shadow run** : possédé par le `ShadowSimulationRunner` (Couche 05),
  le baseline « agriculteur passif ». Son scénario est dérivé par
  `ScenarioContext.CreateFrozenShadowFrom` : les paramètres EXOGÈNES
  (température, précipitations, MAEC, PSE) sont **partagés par référence**
  avec le run réel — donc climat et politiques suivent en lockstep même
  si l'utilisateur les change en cours de run — tandis que les paramètres
  de DÉCISION agriculteur (arrachage de haies, intensité d'intrants,
  couverts, restitution résidus) sont **gelés** à leur valeur au
  lancement / reset.

Le shadow avance d'un tick chaque fois que le run réel émet son événement
`TickCompleted`, via `SimulationEngine.TickWithoutAdvancingScenario` : le
scénario partagé (exogène) a déjà été avancé par le `Tick()` du run réel,
on ne le double-avance donc pas, et les valeurs agriculteur gelées restent
constantes.

**Garanties** :

- Même `SeededRandom` (seed maître identique, dérivation par hash
  identique pour les sous-systèmes) : tout aléa des règles est reproduit
  à l'identique dans les deux runs.
- Conditions exogènes partagées (climat, politiques) : seules les
  décisions agriculteur peuvent écarter les deux trajectoires.
- Tant qu'aucune décision ne les diverge, `ShadowModel` égale le modèle
  réel à chaque tick et le KPI lit **0 par construction** — la lecture
  honnête « la techno ne change encore rien ».

Le Hero KPI « apport de la techno » = intégrale jour-par-jour de l'écart
de rentabilité intégrée `(réel − shadow)` en €/ha depuis le jour 0
(part GROSSE, `CumulativeTechValueIndicator`), **NET** de l'investissement
de capital cumulé des actions (`DecisionJournal.TotalInvestmentEurosPerHectare`,
soustrait au site de publication ; coûts capteurs exclus). L'**horizon de
rentabilité** est le premier jour où ce NET atteint le point mort
(NET ≥ 0), à condition qu'un investissement existe à amortir.

---

## 7. Conventions de nommage et d'organisation

### Classes

- `PascalCase` pour les noms de classes, types et méthodes publiques.
- `_camelCase` pour les champs privés.
- Suffixes explicites :
  - `*ScriptableObject` n'est pas nécessaire (le type est implicite par
    le namespace `Data.RuntimeContainers`).
  - `*Event` pour les classes d'événements de modèle (Couche 02),
    consommées via l'`EventLog`.
  - `*EventBus` (suffixe distinct) réservé aux signaux UI ponctuels de la
    Couche 05 (ex. `SensorClickedEventBus`).
  - `*Binding` pour les MonoBehaviours de la Couche 5 qui écoutent un
    observable.
  - `*Rule` pour les règles biophysiques de la Couche 1.
  - `*Sensor` pour les implémentations de capteurs.

### ScriptableObjects observables

- Nom de fichier asset : `RC_<Domain>.asset` (ex.
  `RC_HedgerowDensity.asset`, `RC_Biodiversity.asset`).
- Stockés dans `Assets/_Project/Data/RuntimeContainers/`.
- Pattern : champ privé sérialisé + getter public + méthode `Set(value)`
  qui invoque `OnChanged`.

### Événements de modèle (Couche 02)

- Classes immutables, suffixe `Event` (ex. `DroughtProlongedEvent`,
  `FaunaAcousticAnomalyEvent`, `SoilCarbonLowEvent`,
  `LowProfitabilityEvent`).
- Stockés dans `Assets/_Project/02_Sensors/Events/`.
- Consommés via l'`EventLog` append-only (pas d'EventBus) : l'engine
  réel les ajoute, le `RecommendationEngine` et le panneau décision les
  lisent.

### Asmdef

- Un asmdef par couche, nommé `Bocage.<Layer>` (ex.
  `Bocage.SimulationCore`, `Bocage.Sensors`, etc.).
- Références strictes définies dans le fichier asmdef.

### Scènes et hiérarchie

- Scène unique : `Main.unity` dans `Assets/_Project/`.
- 7 racines préfixées `_` (cf `CLAUDE.md` §8).

### Logging

- Pas de `Debug.Log` direct. Utiliser `SimLogger.DebugLog`,
  `SimLogger.SimulationLog`, `SimLogger.UserActionLog`.
- Format : `[<Layer>] <message> {context: ...}`.

### Tests

- Stockés dans `Assets/_Project/Tests/EditMode/`.
- Asmdef de tests référence uniquement la Couche 1 (les tests EditMode
  ne touchent pas Unity runtime).
- Nommage : `<ClassUnderTest>Tests.cs`.

---

## 8. Inventaire des classes par chantier (E1-E9)

Les chantiers E1 à E9 sont livrés. Cette section ne maintient plus de
liste parallèle « classes prévues par chantier » : c'était précisément ce
qui dérivait du code. L'inventaire de référence par chantier vit désormais
dans `docs/BACKLOG.md` (statut des items) et `docs/CALIBRATION.md`
(constantes et dérivations, avec le détail E8/E9). Les classes réelles par
couche sont décrites en §2 ci-dessus.

**Faits de calibration E8 (refonte delta tech)** à retenir, dérivation
complète dans `CALIBRATION.md` :

- `CropYieldDynamicsRule.ComputeIntensityEffect` est CONCAVE
  (quadratic-plateau / réponse azotée Mitscherlich, courbure
  `IntensityCutCurvature = 0.70`) : sous l'intensité de référence (1.0) la
  pénalité de rendement croît avec le CARRÉ de la profondeur de coupe
  (−2.8 % à I=0.8, −17.5 % à I=0.5) ; au-dessus, la réponse plafonne.
- `InputCostDynamicsRule` n'indexe que la part VARIABLE des charges sur
  l'intensité (`VariableCostShare = 0.30`, soit 70/30 fixe/variable) : la
  part structurelle fixe ne recule pas quand on extensifie.
- De ces deux courbures émerge un optimum de profit autour de I* ≈ 0.81
  (ni l'extensif maximal ni l'intensif maximal ne maximisent la marge),
  ce qui donne du sens aux contre-recommandations économiques de la
  Couche 03.

---

## 9. Récap impact architecture

Les chantiers E1 à E9 **n'ont cassé aucune architecture existante**. Les
5 couches restent strictement empilées, les boundaries asmdef respectées,
sans aucun retournement de dépendances. Le boundary Unity / pure C# est
respecté intégralement (les classes de delta tech et de recommandation
restent en C# pur Couches 03/04 ; seuls leurs orchestrateurs —
`SimulationRunner`, `ShadowSimulationRunner`, `AutoActionApplier` — vivent
en Couche 05).

Le détail des classes ajoutées par chantier n'est plus tenu ici (cf §8) :
l'inventaire de référence vit dans `docs/BACKLOG.md` et
`docs/CALIBRATION.md`, et les classes réelles par couche sont décrites
en §2.
