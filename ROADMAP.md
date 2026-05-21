# ROADMAP.md — Plan de production

10 étapes verticales, chacune avec un livrable démontrable. Chaque étape
peut être un point de coupe propre si le scope déborde.

**Statut global** : en cours — étapes 1 à 6 livrées (simulation core,
scène data-driven, 2 Hero KPIs honnêtes + 3 placeholders, dashboard
UI Toolkit complet, capteurs visibles avec hover sync scène ↔ liste).
Prochaine étape : 7 (presets + scénario + KPI rentabilité honnête).

---

## Étape 1 — Bootstrap projet

**Objectif** : repo, documentation et structure de dossiers en place.

**Livrables**

- 9 fichiers de documentation (`README.md`, `CLAUDE.md`, `DECISIONS.md`,
  `ARCHITECTURE.md`, `ROADMAP.md`, `WEBGL_GOTCHAS.md`,
  `ASSETS_LIST.md`, `LICENSE`, `.gitignore`).
- Arborescence de dossiers Unity (vide, avec `.gitkeep`).

**Critère de validation**

- Repo public sur GitHub avec structure visible.
- README rendu correctement sur la page repo.

**Estimation** : 0.5 jour.

**Statut** : ✅ livré.

---

## Étape 2 — Architecture squelette

**Objectif** : projet Unity 6 créé, asmdef en place pour les 5 couches,
scène `Main` avec 7 racines préfixées, bootstrap minimal.

**Livrables**

- Projet Unity 6 LTS configuré (URP 2D, build target WebGL).
- 5 asmdef (`Bocage.SimulationCore`, `Bocage.Sensors`,
  `Bocage.Decision`, `Bocage.Indicators`, `Bocage.Presentation`) avec
  références strictes.
- Scène `Main` avec les 7 racines préfixées `_`.
- `_Bootstrap` MonoBehaviour qui logue "bootstrap OK" via `SimLogger`.
- `SimLogger` à 3 niveaux fonctionnel.
- Player Settings WebGL configurés (IL2CPP, stripping High, Brotli).

**Critère de validation**

- Build WebGL passe en local.
- Hiérarchie de scène conforme.
- Tentative d'ajout d'un `using UnityEngine;` dans la Couche 1 → erreur
  de compilation.

**Estimation** : 1 jour.

**Statut** : ✅ livré.

---

## Étape 3 — Simulation core minimaliste

**Objectif** : Couche 1 fonctionnelle avec un modèle d'écosystème
minimal et quelques règles biophysiques. Tests unitaires en place.

**Livrables**

- `SimulationEngine` avec coroutine de tick (1 tick = 1 jour).
- `EcosystemModel` avec : nappe phréatique, densité haies, météo
  (température, précipitations).
- 3 à 5 `BiophysicalRules` (croissance haies, dynamique nappe, impact
  pluie sur sol).
- `SeededRandom` avec sous-seeds par hash.
- `ScenarioContext` avec presets initiaux.
- `TransitioningParameter<T>` fonctionnel.
- 5 tests EditMode minimum (déterminisme, conservation, dynamique
  nappe).

**Critère de validation**

- Tests passent en EditMode.
- Lancement Play Mode : log de progression du modèle dans la console
  via `SimLogger`.

**Estimation** : 1.5 jour.

**Statut** : ✅ livré (SimulationEngine, EcosystemModel, 4 règles
biophysiques, SeededRandom, ScenarioContext, TransitioningParameter,
6 fichiers de tests EditMode).

---

## Étape 4 — Scène visuelle minimaliste

**Objectif** : scène 2D affiche un paysage statique reconnaissable du
Perche.

**Livrables**

- Sprites background, midground, foreground en place (versions
  provisoires si Nanobanana pas prêt).
- Composition de scène avec ordre de rendu correct.
- Shader sky (gradient ciel) en Shader Graph, paramétrable.
- Caméra orthographique fixe configurée.
- Composition validée en Play Mode.

**Note d'architecture** (cf DECISIONS.md #36, #37, #38) : composition
data-driven via `SceneCompositionDefinition` (ScriptableObject) lu par
`SceneAssembler` au boot ; tous les shaders runtime sont des Shader
Graph (`SG_Sky` à l'Étape 4, puis `SG_Hedgerow`, `SG_Pond`, `SG_Meadow`
à l'Étape 5+) ; 7 sorting layers déclarés (Sky → FX).

**Critère de validation**

- Scène lisible, esthétiquement cohérente avec la direction artistique.
- Build WebGL toujours fonctionnel.

**Estimation** : 1.5 jour.

**Statut** : ✅ livré.

**Ajouts d'architecture en cours de route** :
- `Camera.rect` viewport pour scène centrée + marges UI sur les 4 côtés
  (à remplir à l'Étape 6) ; appliqué en Edit Mode via `[ExecuteAlways]`
  pour parité Edit/Play du cadrage.
- Workflow `Rebuild from Composition` ↔ `Capture Scene → Composition`
  via inspector custom : l'artiste manipule les sprites dans la Scene
  view (W = move, R = scale) et capture les transforms vers le
  ScriptableObject. Source de vérité = l'asset.
- Sous-dossier `_Scene_Visual/Composition` pour les sprites spawnés ;
  le `Sky` (sprite + Shader Graph) reste sibling de `Composition` pour
  ne pas se faire nettoyer au boot.
- `ScenicElement.scale` est `Vector2` (non-uniforme) pour gérer les
  sprites bandeaux type `grass_border` sans déformer les autres.

---

## Étape 5 — Liaison simu-visuel + 1er Hero KPI

**Objectif** : démontrer le pipeline complet sur un seul indicateur.

**Livrables**

- ScriptableObject observable `RC_HedgerowDensity`.
- `HedgerowDensityIndicator` (Couche 4) qui lit
  `EcosystemModel.HedgerowDensity` et écrit dans le ScriptableObject.
- `HedgerowDensityBinding` (Couche 5) qui écoute le SO et met à jour un
  texte UI.
- Affichage du KPI à l'écran avec valeur qui évolue en simulation.
- Shader haies (Couche 5) module la couleur des sprites haies en
  fonction du SO.

**Critère de validation**

- Démo : on lance la simu, le KPI bouge, les haies à l'écran réagissent
  visuellement à l'évolution de la densité.

**Estimation** : 1 jour.

**Statut** : ✅ livré (`HedgerowDensityIndicator` Couche 4, SO observable
`RC_HedgerowDensity` avec asmdef dédié `Bocage.Data.RuntimeContainers`,
`SimulationRunner` coroutine de tick, UI Toolkit Dashboard avec
`HedgerowDensityLabelBinding`, Shader Graph `SG_Hedgerow` (Lerp +
Multiply pour conserver le détail texture), `HedgerowShaderBinding`
data-driven via scan par préfixe de nom sous le spawnRoot, 6 tests
EditMode supplémentaires).

**Ajouts d'architecture en cours de route** :
- Nouvel asmdef `Bocage.Data.RuntimeContainers` dans
  `Assets/_Project/Data/RuntimeContainers/` pour isoler les
  ScriptableObjects observables. Référencé par Presentation.
- `ScenicElement.material` (champ `Material` optionnel) ajouté à la
  composition data-driven et capturé par l'inspector custom. Sans ça,
  le SceneAssembler respawnait les sprites avec Sprite-Default à chaque
  Play, écrasant les matériaux assignés manuellement.
- `HedgerowShaderBinding` ne référence plus les `SpriteRenderer` un à
  un (fragile : refs détruites au respawn). Il prend un transform
  spawnRoot + un tableau de préfixes de nom (`hedge_`, `pollard_`),
  scanne à `Start` après que SceneAssembler a fini.

---

## Étape 6 — UI complète et Hero KPIs

**Objectif** : tableau de bord complet en place avec les Hero KPIs
honnêtes câblés et l'architecture UI prête à accueillir les KPIs
reportés (cf DECISIONS.md #40).

**Sous-étape 6a — Backend KPIs honnêtes** : ✅ livré.
- Indicateur Couche 4 `WaterTableIndicator` (lecture directe de
  `EcosystemModel.WaterTableDepth`, normalisation inversée).
- Container `RC_WaterTableDepth` (pattern observable).
- Extension `SimulationRunner` (2 slots de publication, 1 par KPI
  honnête).
- Binding `WaterTableLabelBinding` (UI Toolkit, fail-soft tant que le
  label UXML n'existe pas).
- 6 tests EditMode sur l'indicateur.

**Sous-étape 6b — Dashboard étoffé** :
- Layout dark mode complet (Garamond + JetBrains Mono).
- Hero strip à 5 cartouches dans l'ordre fixé par DECISIONS.md #39 :
  `Haies → Nappe → Biodiversité → Rentabilité → Delta tech`. Les 2
  premières affichent les valeurs honnêtes ; les 3 dernières sont des
  placeholders "à venir" libellés avec l'étape d'arrivée (7 ou 8).
- 3 panneaux Niveau B (Biodiversité, Climat & ressources, Économie).
  Les colonnes affichent uniquement les sous-indicateurs honnêtes
  câblables aujourd'hui (la valeur Nappe ré-utilisée dans Climat &
  ressources). Les autres lignes sont placeholders "à venir".
- Tooltips Garamond italique sur hover des cartouches.
- Bandeau d'avertissement si fenêtre < 1280 px.

**Sous-étape 6c — Capteurs et liste capteurs** : ✅ livré.
- 5 sprites capteurs (piézomètre, station météo, tour de covariance,
  acoustique, piège photo) intégrés dans la scène via le système
  data-driven `SensorPlacementDefinition` SO + `SensorVisualPlacer`
  (calqué sur le pattern composition/SceneAssembler).
- 2 capteurs marqués Online (piézo → WaterTableDepth, météo →
  CurrentWeather), 3 marqués Deferred (cf DECISIONS.md #40 — refus
  d'inventer une variable mesurée).
- La minimap vectorielle initialement prévue est remplacée par un
  panneau "Capteurs déployés" listant chaque capteur avec dot statut,
  nom, type et variable observée (ou étape d'arrivée pour Deferred).
  Rationale : la scène étant visible derrière l'UI, une carte
  spatiale dupliquait l'info ; une liste structurée est plus dense.
- Hover sync bidirectionnel via `SensorHoverEventBus` statique
  (CLAUDE.md §6) : pointer entre un sprite scène scale 1.0 → 1.15
  et highlight la rangée correspondante, et inverse.

**Critère de validation**

- Toute l'UI est en place et lisible.
- Build WebGL < 30 MB toujours respecté.
- Démo : 2 KPIs honnêtes bougent au tick, 3 placeholders affichent
  proprement "à venir Étape 7" / "à venir Étape 8".
- Aucun chiffre inventé à l'écran (cf principe de primauté du
  capteur, CLAUDE.md §9).

**KPIs reportés et leurs étapes d'arrivée** (cf DECISIONS.md #40) :
- `IntegratedProfitability` → arrive à l'Étape 7 quand le modèle
  expose `CropYield`, `InputCost`, `MaintenanceCost`.
- `BiodiversityComposite` → arrive à l'Étape 8 quand le modèle
  expose `FaunaPopulation` (et idéalement diversité végétale).
- `TechDelta` → arrive à l'Étape 8 quand la shadow run est câblée.

**Estimation** : 6a (0.5 j) + 6b (~0.5 j) + 6c (~0.5 j) = ~1.5 j
livrés.

**Statut** : ✅ livré (6a, 6b et 6c).

---

## Étape 7 — Système de presets et casquette Scénario

**Objectif** : l'utilisateur peut régler le contexte scénario.

**Livrables**

- Scenario panel UI avec 4 curseurs (climat, pression agricole,
  contraintes réglementaires, horizon).
- ScriptableObjects de presets dans `Data/ScenarioPresets/`.
- Application des presets via `TransitioningParameter<T>` (interpolation
  7-14 jours simulés).
- Persistance PlayerPrefs de la dernière configuration de presets.
- Boutons play/pause/x1/x10/skip-to-end fonctionnels.
- **Variables d'état économiques** ajoutées à `EcosystemModel` :
  `CropYield`, `InputCost`, `MaintenanceCost`. Règles biophysiques /
  économiques associées (rendement modulé par densité haies et nappe,
  coûts intrants modulés par pression agricole).
- **Hero KPI `IntegratedProfitability`** câblé honnêtement : indicateur
  Couche 4 + container `RC_IntegratedProfitability` + binding label
  remplaçant le placeholder "à venir Étape 7" du hero strip.
- Tests EditMode sur les nouvelles règles économiques et l'indicateur.

**Critère de validation**

- Démo : modification d'un curseur → transition douce visible dans la
  scène et les KPIs.
- Pause / reprise / vitesses fonctionnent.
- Le 4ème cartouche du hero strip (Rentabilité) affiche désormais une
  valeur honnête en €/ha/an dérivée de l'état modèle.

**Estimation** : 1.5 jour (était 1 j, +0.5 j pour le KPI économique
honnête reporté depuis l'Étape 6).

**Statut** : à faire.

---

## Étape 8 — Système de décisions et casquette Recommandations

**Objectif** : moteur de décision riche en place, recommandations
arbitrables par l'utilisateur, comparaison shadow run fonctionnelle.

**Livrables**

- `EventDetector` (Couche 2) détecte au moins 3 types d'événements
  (chalara, sécheresse prolongée, anomalie acoustique).
- `RecommendationEngine` (Couche 3) produit recommandations à partir
  des événements.
- `OutcomeProjector` avec incertitudes (distributions) et 2 horizons
  (court / long terme).
- `AutoActions` appliquées en real run.
- `DecisionJournal` append-only.
- Decision panel UI avec recommandations à arbitrer (accepter / rejeter).
- `ShadowSimulationRunner` opérationnel (run parallèle, mêmes seeds,
  `applyTechActions = false`).
- **Variable d'état `FaunaPopulation`** ajoutée à `EcosystemModel`
  (avec couplage haie/proie/prédateur minimal). Règles de dynamique
  associées. Tests EditMode dédiés.
- **Hero KPI `BiodiversityComposite`** câblé honnêtement : indicateur
  Couche 4 agrégeant `HedgerowDensity`, `WaterTableDepth` et
  `FaunaPopulation`, container `RC_BiodiversityComposite`, binding
  label remplaçant le placeholder "à venir Étape 8" du hero strip.
- **Hero KPI `TechDelta`** câblé honnêtement : différence en % entre
  l'agrégat de bien-être écosystémique de la real run et de la
  shadow run. Container `RC_TechDelta`, binding label remplaçant le
  placeholder "à venir Étape 8" du hero strip.
- Vue de comparaison real vs shadow.
- **Panneau "Conditions initiales"** : section UI dédiée pour
  paramétrer l'état du bocage AVANT le démarrage du run :
  `HedgerowDensity` (m/ha), `WaterTableDepth` (m), `FaunaPopulation`
  (densité agrégée). Édition autorisée uniquement quand
  `SimulationRunner.CurrentDay == 0` ; gelée dès le premier tick. Un
  bouton "Réinitialiser le bocage" reconstruit `EcosystemModel` avec
  les valeurs courantes du panneau (et remet le compteur de jours à
  0). Cohérence visuelle : la modification de `HedgerowDensity` doit
  recomposer le placement des sprites de haies via
  `SceneCompositionDefinition` pour respecter le principe de
  primauté du capteur (CLAUDE.md §9) — choix à arbitrer à
  l'implémentation entre (a) regénérer la liste de sprites, (b)
  modifier l'opacité du shader haies, (c) garder la composition fixe
  et ne permettre que de petites variations numériques.

**Critère de validation**

- Démo : un événement chalara apparaît, une recommandation s'affiche,
  l'utilisateur arbitre, l'effet sur les KPIs diverge entre real et
  shadow.
- Outcomes projetés visibles avec barres d'incertitude.
- Démo : changement de la densité de haies dans le panneau "Conditions
  initiales" + clic "Réinitialiser le bocage" → la scène et les KPIs
  reflètent le nouvel état de départ. Tentative d'édition après day=1
  → champs grisés, message explicatif.

**Estimation** : 2.5 jours (était 2 j, +0.5 j pour le panneau
Conditions initiales).

**Statut** : à faire.

---

## Étape 9 — Tous les effets visuels et faune

**Objectif** : scène vivante avec tous les sprites finaux, modulation
visuelle pilotée par le modèle.

**Livrables**

- Tous les sprites finaux générés via Nanobanana et post-traités.
- Faune en pool (hirondelle, chouette, busard, héron, amphibien) avec
  patterns d'animation simples.
- Densité de faune pilotée par l'index de biodiversité.
- Shader haies, mare, prairie pilotés par variables modèle (humidité,
  healthT, niveau d'eau).
- Effets Niveau 3 : modulation `healthT` sur faune et haies.
- Particules Unity (feuilles dérivantes, poussières dans la lumière).

**Critère de validation**

- Démo : scène riche, faune pool tourne, les effets visuels suivent
  les variables du modèle (vérifié par audit primauté du capteur).

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Étape 10 — Polish, optimisation, déploiement final

**Objectif** : version portfolio livrable, déployée sur GitHub Pages.

**Livrables**

- Workflow GitHub Actions (`game-ci/unity-builder`) qui build et
  déploie sur la branche `gh-pages`.
- Build WebGL final < 30 MB (compressé Brotli).
- Time-to-interactive < 10 s vérifié.
- 60 FPS stable vérifié.
- Polish UI final (alignements, marges, hovers).
- README finalisé avec démo link, GIF hero, screenshots.
- `SessionReporter` opérationnel et accessible depuis l'UI.
- Audit final : aucune violation de la primauté du capteur.

**Critère de validation**

- Démo accessible publiquement sur `https://<user>.github.io/<repo>/`.
- README avec liens vivants.
- Build CI vert.

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Total et marges

- Somme brute : 13 jours-équivalents IA-assistés.
- Marge réaliste × 1.3 : **~17 jours**.

---

## Stratégie de coupe en cas de dépassement

Ordre de coupe (du plus acceptable au plus douloureux) — cf `CLAUDE.md`
§17 :

1. Implémentation décision **moyenne** au lieu de riche (réduire
   incertitudes, un seul horizon).
2. Suppression des effets visuels Niveau 3 (modulation `healthT` sur
   faune et haies).
3. Réduction tests unitaires de 5-10 à 3-5.
4. Réduction sprites uniques de 15 à 10 (fusion de variantes).
5. **NE PAS COUPER** : architecture 5 couches, organisation Git,
   cohérence du pipeline assets, polish UI final.

---

## Règle d'or

À **70 % du temps écoulé**, le projet doit être à **~85 % fonctionnel**.

Sinon, déclencher la stratégie de coupe immédiatement, dans l'ordre
ci-dessus. Ne pas espérer rattraper en sprint final.

Vérification recommandée à la fin de l'Étape 7 (~70 % de la roadmap) :
si l'UI complète n'est pas en place, couper.
