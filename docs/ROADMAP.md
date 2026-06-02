# ROADMAP.md — Plan de production MVP

Document réécrit le 2026-05-28 après session de recadrage externe.
Remplace la roadmap historique en 10 étapes verticales (étapes 1 à 10
livrées comme MVP technique, voir Annexe « Historique »).

---

## 1. Cadre

**Scope MVP verrouillé** : cf `CLAUDE.md` §17. Audience prioritaire =
recruteurs tech + jury M1, cible 150 h, principe directeur « rien en
scène ou en code ne donne le goût d'inachevé ».

**Discipline opérationnelle** : cf `CLAUDE.md` §18. 8 règles
engageantes, dont en particulier :

- Règle 1 — Branches dédiées par chantier : `feature/E{N}-{nom}`.
- Règle 3 — Validation utilisateur avant modif majeure.
- Règle 4 — Tests EditMode obligatoires (aucun merge sans tests verts).
- Règle 5 — `BACKLOG.md` mis à jour à chaque chantier.
- Règle 6 — Commits séparés code / doc.
- Règle 8 — Compléter ou supprimer (jamais laisser en l'état).

**Pas de stratégie de coupe pré-décidée** (cf ADR #56). Arbitrage au
cas par cas en cas de dépassement.

---

## 2. Vue d'ensemble — 7 chantiers E1-E7

Chaque étape est un chantier autonome sur sa propre branche
`feature/E{N}-{nom}`. Estimations basses-hautes.

| # | Chantier | Branche | Estimation | ADR cadrants |
|---|---|---|---|---|
| E1 | Cleanup chalara + refactor actions manuelles | `feature/E1-cleanup-chalara` | 5-8 h | #46, #47, #55 |
| E2 | Saisonnalité + WeatherStation chaîne complète | `feature/E2-saisonnalite` | 16-22 h | #52 |
| E3 | Carbone sol + EddyTower bout-en-bout | `feature/E3-carbone-sol` | 10-14 h | #48 |
| E4 | Faune visible 4 espèces | `feature/E4-faune-visible` | 10-13 h | #49 |
| E5 | Capital + horizon rentabilité + biodiv 3 facteurs | `feature/E5-capital-biodiv` | 12-16 h | #50, #51 |
| E6 | Panneau inspection capteurs + 3 onglets Niveau B remplis ✅ livré 2026-06-02 | `feature/E6-panneau-onglets` | 22-33 h | #53, #54, #57 |
| E7 | Polish + publication MVP | `feature/E7-polish-publication` | 6-10 h | — |

**Total estimé : 81-116 h.** Marge confortable sur cible 150 h.

---

## 3. Étape E1 — Cleanup chalara + refactor actions manuelles

**Branche** : `feature/E1-cleanup-chalara`.
**ADR cadrants** : #46 (purge chalara), #47 (refactor actions
manuelles via journal), #55 (pattern rationale).
**Estimation** : 5-8 h.
**Pré-requis** : aucun (chantier d'entrée).

### Livrables

**Cleanup chalara (DNF 2)** :

- Suppression `Assets/_Project/02_Sensors/Events/HedgeChalaraEvent.cs`.
- Suppression branche pénalité chalara dans
  `HedgerowHealthIndicator.Compute()` + constante `ChalaraPenalty`.
- Suppression branche `case HedgeChalaraEvent` dans
  `RecommendationProvenance.SensorDisplayFor()`.
- Réécriture des 6 tests EditMode utilisant `HedgeChalaraEvent` :
  remplacer `hedge-chalara#NN` par `drought-prolonged#NN` et
  `PlantHedgesRecommendation` par `IrrigationAdviceRecommendation`
  comme fixture (préserve la couverture sur supersession et dedup).
- BACKLOG : items #14 et #16 historiques remplacés par item
  « Cadre santé végétale complet ».

**Astuce** : le stash `stash@{0}` contient des patches cleanup
chalara partiels récupérables via `git stash show -p stash@{0}` à
utiliser pour ne pas refaire le travail.

**Refactor actions manuelles via journal (DNF 3 / ADR #47)** :

- `SimulationRunner.ApplyManualXxx()` → créent une `IRecommendation`
  avec `DefaultVerdict = AutoAccepted` et l'ajoutent au journal via
  `DecisionJournal.Append()`.
- `AutoActionPipeline.Apply()` reste seul à modifier le modèle.
- Convention `Id` : `manual-plant-hedges#<day>` /
  `manual-irrigation#<day>` / `manual-reduce-inputs#<day>`.
- Convention `TriggeredByEventId = null` + fallback
  `RecommendationProvenance.Format()` « Action déclenchée par
  l'utilisateur le jour X ».
- Cumulables (manuelle = AutoAccepted, pas de supersession des
  autres entrées du même type).

**Pattern rationale uniforme (ADR #55)** :

- Réécriture des libellés des 3 actions manuelles + 2 recos auto au
  pattern « Title court + Rationale d'action concrète + ligne
  `Effet modélisé : ...` ». Wordings exacts dans ADR #55.
- Pour les 2 recos auto : ligne supplémentaire `Déclenché par : ...`.

### Tests EditMode

- 6 tests `DecisionJournalTests` adaptés (cf cleanup).
- 1 nouveau test : action manuelle correctement journalisée comme
  `AutoAccepted` avec `TriggeredByEventId == null`.
- 1 nouveau test : 2 actions manuelles du même type cumulent leurs
  effets sans supersession.

### Critère de validation

- Tous tests EditMode verts.
- Démo : clic « Planter haies » + clic « Planter haies » →
  2 entrées journal, densité +60 m/ha.
- Démo : reco auto sécheresse → popup affiche rationale au format
  uniforme avec ligne `Effet modélisé : ...` et `Déclenché par : ...`.
- Aucune trace résiduelle de `chalara` dans le code (audit grep).

---

## 4. Étape E2 — Saisonnalité + WeatherStation chaîne complète

**Branche** : `feature/E2-saisonnalite`.
**ADR cadrant** : #52.
**Estimation** : 16-22 h (16 h base + 3 h extension CropYield/InputCost
+ 6-10 h niveau 3 Markov).
**Pré-requis** : E1 mergé (clean baseline).

### Livrables

**Données saisonnières (CALIBRATION.md)** :

- Encodage des normales Météo-France 1991-2020 station
  Mortagne-au-Perche (61) : 12 valeurs T° + 12 valeurs précip.
- Paramètres Markov mensuels : `p_wet` (probabilité jour pluvieux),
  `mu` et `sigma` (paramètres log-normale intensité).
- Détails dans `docs/CALIBRATION.md`.

**Couche 01 — Simulation Core** :

- `SeasonalWeatherDataAsset.cs` : ScriptableObject contenant les
  12 valeurs T° + 12 valeurs précip + paramètres Markov mensuels.
- `MarkovRainModel.cs` : chaîne Markov ON/OFF + log-normale intensité,
  sous-flux RNG dédié `"markov-rain"`.
- Refonte `WeatherUpdateRule` :
  1. Lit `SeasonalWeatherDataAsset[mois courant]`.
  2. Applique anomalies scenario (TemperatureAnomalyC,
     PrecipitationAnomalyPercent).
  3. Tire Bernoulli(p_wet) → jour pluvieux ou sec.
  4. Si pluvieux : LogNormal(mu, sigma) → mm/jour.
  5. T° : T_mois + bruit gaussien (σ = 2 °C), sous-flux RNG
     `"weather-noise"`.
- Extension `CropYieldDynamicsRule` + `InputCostDynamicsRule` à la
  météo journalière (option a) : terme dépendant de la météo réelle,
  pas seulement des anomalies scenario (canicule WeatherStation →
  effet économique direct).

**Couche 02 — Sensors** :

- `WeatherStationReader.cs` : mesure pure T° + précip avec bruit
  gaussien. Pas d'événement, pas de reco.
- Stockage sliding window 365 j (mutualisé avec E6).

**Couche 05 — Presentation** :

- Widget « Mois de démarrage » (combo Jan-Déc) dans section
  « Conditions initiales » du dashboard. Reset only at `CurrentDay == 0`.
- `MonthSelectorBinding.cs`.

### Tests EditMode

- 4-6 tests : déterminisme Markov, distribution mensuelle plausible
  (moyenne 12 mois ≈ normales), bornes T° plausibles (5-19 °C avec
  bruit), reproductibilité sous-flux RNG.

### Critère de validation

- Tests EditMode verts.
- Démo : démarrer en janvier → T° autour 4 °C, peu de pluie ;
  démarrer en juillet → T° autour 19 °C, pics ponctuels (Markov ON).
- Démo : pendant un run, succession de jours secs puis épisodes
  pluvieux (visualisable via panneau inspection WeatherStation après
  E6).
- Aucune régression sur les tests existants
  (CalibrationScenarioValidationTests notamment — les 4 scénarios
  restent dans la fenêtre de plausibilité sur 10 ans simulés).

---

## 5. Étape E3 — Carbone sol + EddyTower bout-en-bout

**Branche** : `feature/E3-carbone-sol`.
**ADR cadrant** : #48.
**Estimation** : 10-14 h (incluant panneau inspection EddyTower).
**Pré-requis** : E2 mergé (la dynamique carbone bénéficie de la
température saisonnière pour la minéralisation).

### Livrables

**Couche 01 — Simulation Core** :

- Nouvelle variable d'état `SoilCarbonStock` (tC/ha) dans
  `EcosystemModel`, default 50.
- `SoilCarbonDynamicsRule.cs` : modèle 1-pool `dC/dt = inputs − k·C`,
  `k = 1/40 an⁻¹`. Détails calibration dans `docs/CALIBRATION.md`.
- 2 nouveaux leviers dans `ScenarioContext` :
  `CoverCropsCoveragePercent` (0-100 %),
  `ResidueRestitutionPercent` (0-100 %).

**Couche 02 — Sensors** :

- `EddyTowerSensorReader.cs` : mesure flux net journalier CO2/CH4
  avec bruit gaussien. Sous-flux RNG `"eddy-tower"`.
- Stockage sliding window 365 j (mutualisé avec E6).

**Couche 04 — Indicators** :

- `SoilCarbonIndicator.cs` : lecture pure de `SoilCarbonStock`,
  normalisation pour Hero/onglet.
- `RC_SoilCarbonStock` (Data/RuntimeContainers).

**Couche 05 — Presentation** :

- 2 sliders « Couverts d'interculture » et « Restitution résidus »
  dans scenario panel.
- Pré-câblage de l'affichage dans l'onglet Climat & Ressources (sera
  finalisé en E6).
- Panneau d'inspection EddyTower (chantier E6 finalise l'UI ; la
  donnée et le sliding window sont livrés ici).

### Tests EditMode

- 4-5 tests : équilibre du modèle 1-pool sous inputs constants
  (`C_eq = inputs / k`), inertie 40 ans réaliste, effet couverts
  positif, effet résidus positif, lecture EddyTower cohérente avec
  variations de `SoilCarbonStock`.

### Critère de validation

- Tests EditMode verts.
- Démo : démarrage default 50 tC/ha + leviers couverts 50 % +
  résidus 80 % → stock C augmente progressivement vers
  équilibre 80-100 tC/ha sur ~30 ans simulés.
- Démo : avec intensification intrants 2.0× et couverts 0 % →
  stock C diminue vers 30-40 tC/ha sur ~30 ans.
- EddyTower expose des flux journaliers cohérents (signe et amplitude
  alignés sur le bilan stock).

---

## 6. Étape E4 — Faune visible 4 espèces

**Branche** : `feature/E4-faune-visible`.
**ADR cadrant** : #49.
**Estimation** : 10-13 h.
**Pré-requis** : E2 mergé (saisonnalité débloque des effets faibles
sur fauna utilisés en E5 ; ici on travaille uniquement la couche
visible).

### Livrables

**Couche 05 — Presentation** :

- `TrajectoryDefinition.cs` : `[Serializable]` struct embarqué dans
  `FaunaSpeciesDefinition`. Endpoints `leftPoint` + `rightPoint`
  off-screen, `durationSec`, amplitude + fréquence d'un sinus vertical
  pour briser la monotonie du vol linéaire.
- `FaunaSpeciesDefinition.cs` (ScriptableObject par espèce) : id,
  `Sprite[] frames` (sous-sprites de la sheet animée), `framesPerSecond`
  (wing flap), `appearanceThreshold` sur `RC_BiodiversityComposite`,
  `spawnRateAtMaxBiodiv` (λ_max par trajectoire), sortingLayer/Order,
  `TrajectoryDefinition[] trajectories` (1 pour les espèces solitaires
  comme buse/chouette, 2 pour l'hirondelle → max 2 oiseaux simultanés).
- 4 assets : `FaunaSpecies_Swallow.asset`, `FaunaSpecies_Owl.asset`,
  `FaunaSpecies_Buzzard.asset`, `FaunaSpecies_Heron.asset` dans
  `Assets/_Project/Data/Fauna/`. Le héron a été remis dans le MVP
  2026-05-30 (décision utilisateur révisée : indicateur sentinelle
  permanent qui apparaît au-dessus d'un seuil biodiv haut et fade
  out sinon — pas une traversée).
- `FaunaPlacementDefinition.cs` : SO racine listant les 4 espèces.
- `FaunaPool.cs` : object pooling sans Instantiate runtime (CLAUDE.md
  §6). Awake branche sur `FaunaMotionMode` :
  - **Traversal** (swallow/owl/buzzard) : 1 GameObject **désactivé**
    par trajectoire (4 sprites avec les valeurs MVP : 2 swallow + 1
    owl + 1 buzzard).
  - **StaticAppearance** (heron) : 1 GameObject **actif** à
    `staticPosition`, alpha = 0 au Awake (invisible jusqu'à
    activation par le binding).
  Total : 5 GameObjects pré-instanciés. `ExecutionOrder -9000` pour
  que les bindings default-order trouvent le pool déjà peuplé.
- `FaunaTraversalMotion.cs` (ex-`FaunaIdleMotion`, renommé 2026-05-30
  pour refléter le modèle traversée plutôt qu'idle) : par sprite
  actif en mode Traversal. Lerp X linéaire entre les 2 endpoints
  sur `durationSec`, Y modulé par sinus (amplitude + fréquence +
  phase déterministe), sprite flip horizontal selon direction
  (XOR avec `defaultFacesRight` par espèce), wing flap à FPS
  constant. `SamplePositionAt(elapsed, direction)` exposé pur pour
  tests EditMode.
- `FaunaStaticAppearance.cs` (nouveau 2026-05-30) : par sprite static
  (heron). GameObject toujours actif, visibilité contrôlée par alpha
  via `Mathf.MoveTowards` à la cadence `fadeDurationSec` configurée.
  `SetVisible(bool)` toggle la cible, `TickFade(deltaTime)` exposé
  pour tests EditMode déterministes.
- `FaunaPoolBinding.cs` : observe `RC_BiodiversityComposite`, cache
  `Normalized01` via `OnChanged`. Update branche sur `MotionMode` :
  - **Traversal** : pour chaque pooled sprite, roll Bernoulli
    `p = λ_effective × Δt` où
    `λ_effective = λ_max × max(0, (biodiv − threshold) / (1 − threshold))`.
    Sur succès : `SetActive(true)`, direction 50/50 random, phase
    sin uniforme `[0, 2π)`, le tout déterministe sous `masterSeed`
    via `SeededRandom.DeriveSubStream("fauna_pool_binding")`.
  - **StaticAppearance** : simple toggle
    `p.StaticAppearance.SetVisible(biodiv >= threshold)` — le fade
    timing est géré par le composant lui-même.
  Observation des `RC_FaunaFactor*` non activée en MVP (extensible
  sans casser l'API quand la calibration le demandera).
- `FaunaMotionMode` enum sur `FaunaSpeciesDefinition` : `Traversal`
  (3 oiseaux) ou `StaticAppearance` (héron). Champs SO supplémentaires
  pour static : `staticPosition: Vector2`, `fadeDurationSec: float`.

**Sprites** :

- 4 espèces × 3-4 frames partiellement disponibles dans
  `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/`. Corrections
  finales à charge utilisateur (cf division §2 CLAUDE.md).
- Crunch DXT5 conditionnel (cf E7).

### Tests EditMode

- **7 tests** sur 4 fichiers (`FaunaPoolTests`,
  `FaunaTraversalMotionTests`, `FaunaPoolBindingTests`,
  `FaunaStaticAppearanceTests`) :
  - **Pool** :
    - Traversal : pré-instanciation correcte (count = somme des
      trajectoires × espèces, tous désactivés au sortir du Awake).
    - StaticAppearance : 1 GO unique par espèce static,
      `transform.position = staticPosition`, GO **actif**,
      `TraversalMotion = null`, `StaticAppearance != null`.
  - **Motion** : lerp X linéaire L→R sur 3 checkpoints (0, milieu,
    fin) ; miroir R→L sur les 2 endpoints.
  - **Binding** : `ComputeEffectiveSpawnRate` retourne 0 sous le
    seuil (incluant l'égalité), linéaire au-dessus jusqu'à λ_max à
    biodiv = 1 (3 checkpoints intermédiaires).
  - **StaticAppearance** : `TickFade` lerpe alpha vers cible au
    bon taux (0.25s avec fade 1s → α = 0.25), clamp aux extremums
    0 et 1, réversibilité sur `SetVisible(false)`.

### Critère de validation

- Tests EditMode verts.
- Démo Traversal : biodiv chute progressive → fréquence de passage
  de chaque oiseau décroît, puis tombe à 0 sous le seuil propre
  (swallow 0.30, owl 0.40, buzzard 0.50). Biodiv remonte → les
  passages reprennent.
- Démo StaticAppearance : à biodiv < 0.65, le héron est invisible
  (alpha 0). Quand la biodiv monte au-dessus de 0.65, le héron
  fade in sur 1.5s. Quand elle redescend, fade out symétrique.
  Aucune traversée pour le héron — il reste à
  `staticPosition (2.5, -2.93)` au bord de la mare.
- Aucune `Instantiate`/`Destroy` runtime (Profiler).
- Pas de modulation `_HealthT` sur faune (item BACKLOG #3 hors MVP).

---

## 7. Étape E5 — Capital + horizon rentabilité + biodiv enrichie

**Branche** : `feature/E5-capital-biodiv`.
**ADR cadrants** : #50 (capital), #51 (biodiv 3 facteurs).
**Estimation** : 12-16 h (6-8 h capital + 6-8 h biodiv).
**Pré-requis** : E1 mergé (actions manuelles via journal).

### Livrables

**Capital + horizon (ADR #50)** :

- Champ `InvestmentCost` (€/ha) sur `IRecommendation`.
- Calcul pour `ManualPlantHedgesRecommendation` : densité plantée ×
  prix au m linéaire (paramétré dans `docs/CALIBRATION.md`).
- Texte « Coût upfront estimé : X €/ha » affiché dans popup décision
  (manuel).
- `DecisionJournal.TotalInvestment` (somme cumulée).
- `InvestmentHorizonIndicator.cs` (Couche 04) : calcul années pour
  récupérer l'investissement basé sur `cumulProfitDelta(t) >=
  InvestmentCost`.
- `RC_TotalInvestment`, `RC_InvestmentHorizon` (Data/RuntimeContainers).
- Pré-câblage onglet Économie (finalisé E6).

**Biodiv 3 facteurs exposés (ADR #51)** :

- `FaunaDynamicsRule` refondue : 3 facteurs (habitat, eau, intrants)
  calculés explicitement.
- `RC_FaunaFactorHabitat`, `RC_FaunaFactorWater`,
  `RC_FaunaFactorInputs` (Data/RuntimeContainers).
- Effet faible météo journalière (canicule) sur fauna : pénalité au-
  delà de seuil T° quotidien (sourcé Hallmann 2017).
- Effet faible carbone sol sur fauna : bonus si stock C > seuil (sols
  vivants).
- Recalibration des pondérations du `BiodiversityCompositeIndicator`
  sur base littérature (Vigie-Nature, Hallmann 2017, MNHN 2024).
- `FaunaPoolBinding` (livré E4) peut maintenant observer aussi
  `RC_FaunaFactor*` pour la sélectivité des espèces.

### Tests EditMode

- Capital : 3 tests (calcul `InvestmentCost`, cumul
  `TotalInvestment`, horizon calculé correctement avec fixture
  shadow > real).
- Biodiv : 3 tests (3 facteurs cohérents avec leur input, effet
  canicule, effet carbone sol).

### Critère de validation

- Tests EditMode verts.
- Démo : clic « Planter 30 m/ha » → popup affiche « Coût upfront
  estimé : 1500 €/ha » (densité × prix), entrée journal avec coût.
  Après ~5-15 ans simulés (selon scénario), horizon rentabilité
  affiche une valeur cohérente.
- Démo onglet Biodiv (partiel) : 3 lignes facteur s'affichent ; sous
  scénario sécheresse → composante eau chute ; sous scénario
  intensification → composante intrants chute.

---

## 8. Étape E6 — Panneau inspection capteurs + 3 onglets Niveau B remplis

**Statut** : ✅ livré 2026-06-02.
**Branche** : `feature/E6-panneau-onglets`.
**ADR cadrants** : #53 (panneau inspection), #54 (onglets), #57
(force-online sur tous les capteurs).
**Estimation** : 22-33 h (12-21 h panneau inspection + 10-12 h
onglets).
**Pré-requis** : E2, E3, E4, E5 mergés (toutes les variables source
des onglets et des graphes d'inspection existent).

### Livrables effectifs

**Fondation transverse (commit `b144338`)** :

- Interface `ISensorHistory<T>` (Couche 02) + conteneur générique
  `RollingSensorHistory<T>` (ring buffer pré-alloué, éviction O(1)).
- Rétro-fit `WeatherStationReader` et `EddyTowerSensorReader` :
  délèguent leur historique au conteneur réutilisable. API publique
  inchangée (HistoryCount/CopyHistoryTo + nouveau TryGetLatest).
- 6 tests EditMode `RollingSensorHistoryTests`.

**3 onglets Niveau B (ADR #54, commit `7bd10eb`)** :

- `OngletBiodivBinding` (5 lignes : composite + 3 facteurs +
  comptage espèces visibles via `FaunaPool.PooledSprites`).
- `OngletClimatBinding` (4 lignes capteur : T° moyenne 365 j,
  précip cumulées 365 j, stock C, flux net CO2 — la ligne nappe
  reste pilotée par le binding existant `WaterTableDetailLabelBinding`).
- `OngletEconomieBinding` (7 lignes : rendement + intrants +
  entretien + PSE + PAC + investissement cumulé + horizon — PSE et
  PAC réutilisent les constantes publiques de
  `IntegratedProfitabilityIndicator` pour ne jamais diverger du Hero
  KPI).
- Enrichissement `Dashboard.uxml` : 16 labels nommés ajoutés dans
  les 3 panneaux (5 + 4 + 7).
- 8 tests EditMode `OngletBindingsTests` sur les helpers purs.

**Pivot UX vers modales Niveau B (commit `5e38bda`)** :

- Les 3 panneaux Niveau B affichaient tout leur contenu en
  permanence — perçu comme surcharge visuelle.
- Refonte : chaque panneau devient un `Button` compact en bas
  (`body-row`) qui ouvre une modale centrée (overlay full-screen +
  carte centrale) au clic.
- `NiveauBModalsBinding` (Couche 05) auto-câble les 3 boutons →
  3 overlays via la classe utilitaire `.hidden` éprouvée.
  Fermeture : X, clic en dehors de la carte, Échap.
- 4 tests EditMode sur `SetVisible`.

**B.1 — Readers manquants (commit `9cca833`)** :

- Struct générique `SensorSample<T> { Measured, Truth }` pour stocker
  la paire mesure/vérité dans les historiques.
- `PiezometerReader` (Couche 02) : observe `WaterTableDepth`, bruit
  gaussien σ = 0.05 m, sous-stream RNG `"piezometer"`, historique
  365 j. Personne d'autre ne consomme le reader — les indicateurs
  continuent à lire la vérité du modèle (même pattern que
  `WeatherStationReader`/`EddyTowerSensorReader`).
- `AcousticSensorReader` + `CameraTrapSensorReader` (Couche 02) :
  wrappers d'historique purs. `FaunaSensorReader` refondu en
  orchestrateur — possède les 2 sous-readers, expose les propriétés
  publiques `Acoustic`/`Camera`, garde son sous-stream
  `"fauna-sensors"` et son ordre de tirage acoustique-puis-camera
  bit-pour-bit identiques (préserve les tests
  `CalibrationScenarioValidationTests` sur 10 ans).
- `SimulationRunner` : instancie `PiezometerReader` (Awake +
  Rebuild), appelle `ReadAndRecord` dans TickLoop + FastForwardTo,
  expose `Piezometer` et `FaunaSensor` en propriétés publiques.
- 10 tests EditMode (`PiezometerReaderTests` + `FaunaSensorChannelsTests`).

**B.2 — Infrastructure clic (commit `1665e96`)** :

- `SensorClickedEventBus` (Couche 05, statique) : event
  `Action<SensorType>` — copie du pattern `SensorHoverEventBus`.
- `SensorClickHandler` (MonoBehaviour Couche 05) : `OnMouseDown`
  legacy → publie sur le bus. Pas de `Physics2DRaycaster` requis
  (`OnMouseDown` marche avec `Collider2D` seul, comme le hover).
- `SensorVisualPlacer.BuildFrom` ajoute automatiquement
  `SensorClickHandler` à chaque sprite capteur — aucune action
  manuelle Unity requise côté scène.
- 4 tests EditMode `SensorClickedEventBusTests` (notification,
  fan-out, no-subscriber-safe, désabonnement).

**B.3 — Graphe custom (commit `057d5d5`)** :

- `SensorTimeSeriesChart : VisualElement` avec `generateVisualContent`
  callback utilisant `Painter2D` (Unity 6 LTS). API :
  `AddSeries(color, lineWidth, values)`, `AddThreshold(color,
  lineWidth, value)`, `SetYBounds(min, max)`, `ClearSeries/Thresholds`.
  Chaque mutation déclenche `MarkDirtyRepaint` automatique.
- Helpers statiques purs `XForIndex` et `YForValue` (axe Y inversé
  d'UI Toolkit).
- 8 tests EditMode `SensorTimeSeriesChartTests`.

**B.4 — Panneau modal d'inspection (commit `9bf243d`)** :

- Modal `sensor-inspector-overlay` ajoutée au `Dashboard.uxml`
  (overlay full-screen + carte 600 px + header titre/X + sous-titre
  + chart1 caption + chart1 row (axe Y left + host) + chart2 caption
  + chart2 row (caché par défaut, utilisé seulement par
  WeatherStation) + footer info).
- 11 nouvelles classes USS `.sensor-inspector-*`.
- `SensorInspectorPanelBinding` (Couche 05) : abonné au bus, switch
  sur `SensorType` → appelle `ConfigureFor*` correspondante. 5
  layouts pré-câblés (cf ADR #53 §tableau). Charts instanciés
  programmatiquement dans `TryWire()` et `Add()`és aux hosts UXML
  (évite UxmlFactory boilerplate). Fermeture via X, MouseDown sur
  l'overlay (pas Click — voir Pièges ci-dessous), Échap.
- Trigger UI : `SensorListBinding` étendu — chaque ligne du panneau
  « Capteurs déployés » publie aussi sur le bus au clic, pour offrir
  un trigger UI en plus du clic sprite scène.
- 10 tests EditMode `SensorInspectorPanelBindingTests` (extracteurs
  de buffers, compteur trailing-days, reconstruction normales
  mensuelles).

**Décision pragmatique force-online (ADR #57)** :

- Le concept « capteur en attente » (dot ocre) a été retiré de
  l'UI : `SensorListBinding.BuildRow` applique inconditionnellement
  `.sensor-status-dot--online`, et la légende online/deferred a été
  retirée de `Dashboard.uxml`. Le champ `OnlineStatus` reste dans
  le SO et `SensorMetadataTag` pour réactivation future (item
  backlog « capteur en panne / maintenance »).

### Tests EditMode (état final)

**280 tests EditMode verts** au merge — 226 baseline + 54 ajoutés
pendant E6 (6 RollingSensorHistory + 8 onglets + 4 modales Niveau B +
5 Piezometer + 5 FaunaSensorChannels + 4 SensorClickedEventBus +
8 SensorTimeSeriesChart + 10 SensorInspectorPanelBinding + 4
CollapsiblePanelsBinding interlude abandonné = 54 nets).

### Critère de validation atteint

- Tests EditMode tous verts.
- Démo : clic sprite ou ligne UI sur chacun des 5 capteurs → modale
  d'inspection s'ouvre avec le bon layout + données du reader +
  seuils + footer info. Fermeture via X / clic en dehors / Échap.
- 3 boutons Niveau B en bas → 3 modales avec valeurs vivantes.
- Liste de capteurs : 5 dots verts (force-online), pas de légende
  parasite.

### Pièges rencontrés (à ne pas refaire)

1. **Compound class selectors USS** (`.panel-content.collapsed`)
   ne semblent pas matcher dans cette version d'UI Toolkit / projet.
   Plusieurs tentatives de toggle via classe ont échoué. Pattern
   qui marche : **single-class** `.hidden { display: none; }`
   (utilisée depuis des mois sur `decision-popup-overlay`). Adopté
   pour les modales Niveau B et le panneau d'inspection.

2. **Namespace shadowing** : `Bocage.Presentation.Weather` (le SO
   `SeasonalWeatherDataAsset` qui sert E2) shadow le type
   `Weather` au sein de `Bocage.Presentation.Bindings`. Un alias
   `using Weather = Bocage.SimulationCore.Model.Weather` au niveau
   FICHIER ne suffit pas (priorité inférieure au namespace
   englobant). L'alias doit être placé **dans** le bloc
   `namespace { }` pour primer. Touché 2 fois (`OngletClimatBinding`
   puis `SensorInspectorPanelBinding`).

3. **Race ClickEvent au clic sprite** : `OnMouseDown` legacy du
   sprite scene + UI Toolkit MouseDownEvent sur l'overlay qui
   apparaît dans le MÊME frame → l'event est traité comme un
   click-outside, la modale se ferme immédiatement. Fix retenu :
   `StartCoroutine(ShowOverlayNextFrame)` avec `yield return null`
   avant `RemoveFromClassList("hidden")`. ConfigurePanel reste
   synchrone, juste l'apparition décale d'1 frame.

4. **Accordéon Niveau B échoué** : 5+ tentatives sur un
   collapse/expand des 3 panneaux. Toggle via classe CSS, toggle
   via `style.display`, toggle via `RemoveFromHierarchy` — chaque
   approche laissait 2 lignes résiduelles à l'écran. Cause
   profonde non identifiée (probable quirk renderer UI Toolkit
   spécifique à ce build). Pivot UX → modale pour Niveau B et pour
   l'inspecteur capteurs. Pattern fiable, retenu.

---

## 9. Étape E7 — Polish + publication MVP

**Branche** : `feature/E7-polish-publication`.
**Estimation** : 6-10 h.
**Pré-requis** : E1-E6 mergés (chantiers de fond livrés).

### Livrables

**Sub-étape E7.1 — Mesure build CI** :

- Build CI vert post-merge E6.
- Mesurer taille DL + TTI + FPS sur l'URL Pages déployée
  (`https://paul-des-brosses.github.io/bocage-digital-twin/`).

**Sub-étape E7.2 — Crunch DXT5 conditionnel (ADR Crunch conditionnel)** :

- Si taille build ≤ 30 MB → skip Crunch, doc TODO conservée.
- Si taille build > 30 MB → installer module WebGL Build Support
  local, appliquer Crunch DXT5 Quality 50 sur les sprites les plus
  lourds (priorité paysage > UI > faune) via Override for Web. Cf
  `docs/ASSETS_LIST.md` §6 étape 7.
- Si taille > 35 MB → investigation via Build Report Inspector,
  correction, push, remesure.

**Sub-étape E7.3 — Polish UI léger** :

- Alignements, marges, contrastes, hover states (rien de fancy).
- Bandeau viewport < 1280 px → vérifier non-régression.
- Pas d'animation UI complexe (cf BACKLOG).

**Sub-étape E7.4 — README final + capture** :

- Remplacer placeholders `[TODO: live demo link]` et
  `[TODO: hero GIF or screenshot]` du README par URL Pages réelle +
  GIF capture 10-15 s du DT en action + 2-3 screenshots.
- README en anglais (cf ADR #31).

**Sub-étape E7.5 — Tri docs public/privé** :

- Créer dossier `docs-private/` (listé dans `.gitignore`) si nécessaire.
- Décider quel doc va où (SIMULATION_OVERVIEW.md public ou privé ?
  BACKLOG.md ?). Approche par défaut suggérée : tout en public, sauf
  docs internes de travail.

**Sub-étape E7.6 — Audit final** :

- Tests EditMode tous verts.
- Primauté capteur respectée intégralement (cf CLAUDE.md §9 statut
  post-E2/E3).
- `BACKLOG.md` exhaustif (un futur contributeur peut reprendre tout
  item en < 1 h sans reconstruire le contexte).
- Conformité CLAUDE.md (§17 scope MVP, §18 discipline).

**Sub-étape E7.7 — Tag GitHub v1.0 + release note** :

- Tag `v1.0` sur le commit final de `main` après merge E7.
- Release note GitHub résumant les chantiers E1-E7 et le scope MVP
  livré.

### Critère de validation

- Démo accessible publiquement sur URL Pages.
- README en anglais avec liens vivants (live demo + GIF +
  screenshots).
- Build CI vert.
- Un visiteur du portfolio comprend l'utilité du DT en moins de
  2 min sans devoir lire la doc.
- Tag `v1.0` poussé.

---

## 10. Règle de revue à mi-parcours

À ~70 % du temps écoulé (cible ~105 h sur 150 h), faire un point
d'avancement :

- Étapes E1-E4 doivent être mergées.
- E5 doit être en cours.
- Si ce n'est pas le cas, **arrêter pour réviser le scope avec
  l'utilisateur** (discipline §18 règle 2 — pas de pivot sans
  relecture).

Pas de stratégie de coupe pré-décidée (cf ADR #56). Arbitrage au cas
par cas en cohérence avec le principe directeur §17.

---

## Annexe — Historique des étapes 1-10 (MVP technique livré)

Le projet a livré entre l'Étape 1 et la sub-étape 10b-perf un MVP
technique fonctionnel (architecture 5 couches, simulation core,
sensors, decision, indicators, presentation, build CI/CD WebGL). La
roadmap historique en 10 étapes verticales est conservée pour
traçabilité via `git log` et les ADRs #1 à #44.

État au 2026-05-28 :

- Étapes 1 à 9 livrées intégralement.
- Sub-étape 10a livrée (audit narratif + popup loop fix + supersession
  type-level + interventions ponctuelles + provenance capteur).
- Sub-étape 10b livrée (FaunaSensorReader + retrait chalara du
  détecteur + recal CAP).
- Sub-étape 10b-perf livrée (ProjectSettings WebGL + URP réglés +
  build CI déclenché).

Ce qui restait au 2026-05-28 sous l'ancienne roadmap (10c polish UI,
10d README final, 10e audit final) est repris dans la nouvelle étape
**E7 — Polish + publication MVP**, augmentée des sous-étapes Crunch
conditionnel et tri docs public/privé.

Les 6 chantiers E1-E6 sont des chantiers nouveaux issus de la session
de recadrage du 2026-05-28, qui transforment le MVP technique en MVP
de complétude fonctionnelle (cf CLAUDE.md §17 et ADR #45).
