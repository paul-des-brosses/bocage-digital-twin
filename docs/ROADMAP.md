# ROADMAP.md — Plan de production

10 étapes verticales, chacune avec un livrable démontrable. Chaque étape
peut être un point de coupe propre si le scope déborde.

**Statut global** : en cours — étapes 1 à 8 livrées + sub-étapes 9α
et 9β livrées (shaders mare/prairie pilotés par modèle, modulation
healthT sur les haies via binding). Le reste de l'Étape 9 (faune
statique en pool, animation faune, healthT faune, particules) est
**reporté au backlog** (cf `BACKLOG.md`) pour livrer une v1
fonctionnelle d'abord ; le polish visuel viendra en post-livraison.
Prochaine étape : **10 — Polish, optimisation, déploiement final**,
avec revue de la logique/utilité du Digital Twin et première
publication GitHub Pages.

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

**Statut** : ✅ livré (sub-étapes 7a + 7b + 7c.1 + 7c.2 + 7c.3).
Variables d'état économiques et règles biophysiques livrées en 7a,
Hero KPI Rentabilité câblé honnêtement en 7b, refactor des inputs
scénario en 6 paramètres physiques + calibration sourcée en 7c.1,
système de presets avec 4 scénarios calibrés en 7c.2, contrôles de
vitesse (pause / ×1 / ×5 / ×10 / ×20 / skip-to-end) et compteur de
jours en 7c.3. Le panneau Scénario expose maintenant 6 sliders
physiques avec saisie numérique précise.

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

**Statut** : ✅ livré (sub-étapes 8a + 8b + 8c.1 + 8c.2 + 8c.3 + 8c.4).
Variable d'état `FaunaPopulation` + dynamique livrées en 8a. Hero KPIs
Biodiversité (composite pondéré) et TechDelta (real vs shadow profit)
câblés honnêtement en 8b avec `ShadowSimulationRunner` parallèle.
`EventDetector` Couche 2 avec 3 types d'événements (chalara, drought,
fauna acoustic) en 8c.1. `RecommendationEngine` + `OutcomeProjector`
(2 horizons + 3-point bracket) + `DecisionJournal` append-only en 8c.2.
`AutoActionPipeline` + Decision Panel UI (cards avec accept/reject) en
8c.3, avec polish layout en 2 zones « Cadre extérieur » / « Espace
agriculteur ». Panneau « Conditions initiales du bocage » + bouton
one-click « Lancer / Réinitialiser la simulation » en 8c.4. La
cohérence visuelle entre HedgerowDensity et SceneCompositionDefinition
est restée à l'option (c) — composition fixe — par défaut, à
arbitrer définitivement à l'étape 9 polish visuel.

---

## Étape 9 — Effets visuels et faune (partiellement livrée)

**Objectif** : scène vivante avec tous les sprites finaux, modulation
visuelle pilotée par le modèle.

**Livrables originels** vs statut effectif :

1. ✅ Tous les sprites finaux générés via Nanobanana et post-traités
   (faune, haies, mare, prairie, capteurs, hills_perche).
2. ⏸ **Reporté backlog** : faune en pool (4 espèces) avec patterns
   d'animation simples — cf `BACKLOG.md` items 1 + 2.
3. ⏸ **Reporté backlog** : densité de faune pilotée par l'index de
   biodiversité — cf `BACKLOG.md` item 1 (dépend de #2).
4. ✅ **Sub-étape 9α livrée** : shaders haies (déjà livré Étape 5),
   mare (`S_Pond`) et prairie (`S_Meadow`) pilotés par variables
   modèle.
5. ⚠️ **Sub-étape 9β livrée partielle** : modulation `healthT` sur
   les haies (binding + indicateur dérivé + container observable).
   Le node `_HealthT` dans `SG_Hedgerow.shadergraph` reste à câbler
   manuellement (5 min) — cf `BACKLOG.md` item 5. La même modulation
   sur la faune est reportée — cf `BACKLOG.md` item 3.
6. ⏸ **Reporté backlog** : particules Unity (feuilles dérivantes,
   poussières dans la lumière) — cf `BACKLOG.md` item 4.

**Sub-étape 9α — Shaders mare et prairie** : ✅ livré.
- `SoilMoistureIndicator` (Couche 4) dérive un proxy [0,1] de
  l'humidité du sol depuis `EcosystemModel.WaterTableDepth`.
  Extensible plus tard si on ajoute des précipitations lissées.
- Containers observables `RC_SoilMoisture`.
- Shaders `S_Pond.shader` et `S_Meadow.shader` en HLSL pur (cf
  `DECISIONS.md` #41), mêmes propriétés que SG_Hedgerow côté binding.
- Matériaux `M_Pond.mat` et `M_Meadow.mat` avec couleurs par défaut
  calibrées palette Perche.
- Bindings `PondShaderBinding` et `MeadowShaderBinding` (scan par
  préfixe de nom sous le spawnRoot, `MaterialPropertyBlock` partagé).
- `SimulationRunner` publie les deux indicateurs après chaque tick.
- Tests EditMode : `SoilMoistureIndicatorTests`.

**Sub-étape 9β — HealthT sur les haies** : ✅ livré (code) /
🔧 reste 1 action manuelle Unity.
- `HedgerowHealthIndicator` (Couche 4) dérive un proxy [0,1] de la
  santé des haies depuis la densité courante + événements actifs
  (chalara, drought) dans une fenêtre glissante de 60 jours. Pas de
  variable d'état (cf `DECISIONS.md` #42).
- Container observable `RC_HedgerowHealth`.
- `HedgerowShaderBinding` étendu pour pousser `_HealthT` en plus de
  `_Density`. Unity ignore silencieusement la propriété tant que
  `SG_Hedgerow.shadergraph` ne l'expose pas.
- Tests EditMode : `HedgerowHealthIndicatorTests`.
- **Action manuelle Unity restante** : ajouter le node `_HealthT` au
  Shader Graph `SG_hedgerow.shadergraph` (cf `BACKLOG.md` item 5).

**Critère de validation 9α/9β**

- Démo : on lance la simu ; la mare se ternit / brunit quand la
  nappe descend, la prairie jaunit quand l'humidité baisse, les
  haies réagissent à la densité (déjà l'Étape 5) — le canal healthT
  est branché côté data, en attente du node shader.
- Aucun chiffre inventé, aucun cycle de saison artificiel.
- Tests EditMode passent (Couche 1 + Couche 4 indicateurs).

**Estimation** : 0.7 jour (9α + 9β) — livré.

**Estimation initiale** : 1.5 jour — la partie livrée fait ~50 % du
livrable original, le reste est en backlog explicite.

**Statut** : ⚠️ livraison partielle assumée. Items 2/3/5/6 du
livrable original sont formellement reportés au backlog avec hooks
d'extension propres (binding pattern réutilisable, asmdef en place,
DECISIONS.md à jour).

---

## Étape 10 — Polish logique, déploiement final, première publication

**Objectif** : livrer un Digital Twin **fonctionnel et honnête**, pas
joli (le polish visuel est en backlog explicite). L'accent est mis sur
la qualité de la boucle d'utilité (à quoi sert ce DT, est-il
démontrable en 2 minutes ?) avant la qualité esthétique.

**Sub-étape 10a — Revue de la logique et de l'utilité du Digital Twin**

- Audit du « scénario démo » : un visiteur du portfolio doit
  comprendre en 90 secondes ce que le DT fait et pourquoi il est
  honnête. Identifier les frictions narratives (KPI flottants, popup
  recommandation peu lisible, conditions initiales pas explicables,
  etc.).
- Sécuriser la chaîne causale visible : événement → recommandation →
  arbitrage → impact sur les KPIs et sur la divergence real/shadow.
  Si un maillon est invisible ou confus, il faut le clarifier avant
  publication.
- Audit primauté du capteur : aucun visuel ne doit dépendre du
  calendrier ou d'une logique scénique. Inventaire des
  bindings/shaders, traçage de chaque sortie visuelle à une
  variable du modèle ou à un indicateur.
- Simplifications acceptées si elles aident la lisibilité du DT (un
  KPI redondant peut être masqué, un slider peu utile peut être
  caché derrière un repli "Avancé").

**Sub-étape 10b — Optimisation et build WebGL**

- Vérifier la conformité aux contraintes perf (CLAUDE.md §7) :
  IL2CPP, stripping High, Brotli, ASTC/Crunched DXT, pas de MSAA,
  pas de DoF, pas de threads.
- Mesurer : taille build < 30 MB compressé, TTI < 10 s sur connexion
  résidentielle, 60 FPS stable desktop.
- Couper si dépassement, dans l'ordre de `CLAUDE.md` §17.

**Sub-étape 10c — Polish UI léger (pas visuel)**

- Alignements, marges, contrastes, hover states (rien de fancy).
- Bandeau viewport < 1280 px déjà en place — vérifier qu'il ne casse
  rien en redimensionnement.
- Pas d'animation UI complexe (cf `BACKLOG.md` item 7).

**Sub-étape 10d — README + déploiement GitHub Pages**

- Workflow GitHub Actions (`game-ci/unity-builder`) qui build et
  déploie sur la branche `gh-pages`.
- README finalisé : démo link, GIF hero (capture 10–15 s du DT en
  action), 2–3 screenshots, schéma d'architecture des 5 couches,
  liens vers `BACKLOG.md` et `DECISIONS.md`.
- Premier push de la démo en public.

**Sub-étape 10e — Audit final**

- Re-vérifier que `BACKLOG.md` est exhaustif (un futur contributeur
  doit pouvoir reprendre l'effet visuel reporté en 1 h max sans
  reconstruire le contexte).
- Re-vérifier qu'aucune violation de primauté du capteur n'a été
  introduite en cours de polish.
- Tests EditMode passent tous.

**Hors scope assumé (cf `BACKLOG.md`)** :
- `SessionReporter` accessible depuis l'UI (item 8).
- Animations UI léchées (item 7).
- Tout effet visuel avancé (items 1–6).

**Critère de validation**

- Démo accessible publiquement sur `https://<user>.github.io/<repo>/`.
- README en anglais (cf `DECISIONS.md` #31) avec liens vivants.
- Build CI vert.
- Un visiteur du portfolio comprend l'utilité du DT en moins de 2 min
  sans devoir lire la doc.

**Estimation** : 1.5 jour (10a + 10b + 10c + 10d + 10e).

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
