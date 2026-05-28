# Carnet de câblage de la scène Unity

Document vivant. **Tenu à jour à chaque sub-étape par Claude Code.** Tu y
trouves, pour chaque GameObject de la scène `Main`, les components qui le
portent et les références (drag-and-drop dans l'Inspector) qu'il faut
brancher.

Si tu te perds en Play Mode ou pendant une action manuelle, ouvre ce
fichier d'abord : il dit ce qui est censé exister et où.

> Convention : `→` signifie "dans la rubrique de l'Inspector, glisse
> l'objet de gauche dans le champ de droite".

---

## 1. Racines de la scène (7, préfixées `_`)

D'après CLAUDE.md §8 :

```
_Bootstrap
_Camera
_Scene_Visual
_Scene_Overlays
_UI_Canvas
_Audio          (vide, conservée pour cohérence — pas de son dans le projet)
_Debug
```

---

## 2. `_Bootstrap`

| GameObject enfant | Components | Références à brancher |
|---|---|---|
| `SimulationRunner` | `SimulationRunner` (Couche 5 Presentation) | RCs Hero : HedgerowDensity, WaterTable, IntegratedProfitability, BiodiversityComposite, TechDelta. RCs presentation channels (9α/9β) : `SoilMoisture Container` → `RC_SoilMoisture.asset`, `Hedgerow Health Container` → `RC_HedgerowHealth.asset`. `Shadow Runner` → glisser le GO ci-dessous. |
| `ShadowSimulationRunner` ✨ (sub-étape 8b) | `ShadowSimulationRunner` | `Real Runner` → le `SimulationRunner` ci-dessus (même GO ou autre, peu importe). |
| **`AutoActionApplier` ✨ (sub-étape 8c.3)** | `AutoActionApplier` | `Runner` → `_Bootstrap/SimulationRunner`. Subscribes à `TickCompleted` et applique les recos Accepted/AutoAccepted au real engine seul (le shadow n'est jamais touché → TechDelta bouge). |
| `SimulationTraceRecorder` *(optionnel — diagnostic 7b)* | `SimulationTraceRecorder` | — (s'auto-abonne au TickCompleted du SimulationRunner) |

---

## 3. `_UI_Canvas`

C'est le GameObject qui porte le **`UIDocument`** chargeant `Dashboard.uxml`
+ `PanelSettings_Dashboard`. Toutes les `*Binding.cs` de la couche
Presentation vivent sur ce même GameObject (elles ont toutes
`[RequireComponent(typeof(UIDocument))]`).

### Components attendus sur `_UI_Canvas`

| Component | Champs sérialisés à brancher |
|---|---|
| `UIDocument` | Source Asset → `Dashboard.uxml` ; Panel Settings → `PanelSettings_Dashboard.asset` |
| `HedgerowDensityLabelBinding` | (vérifier — voir source) |
| `WaterTableLabelBinding` | (vérifier — voir source) |
| `WaterTableDetailLabelBinding` | (vérifier — voir source) |
| `IntegratedProfitabilityLabelBinding` | `Runner` → `_Bootstrap/SimulationRunner` |
| **`BiodiversityLabelBinding` ✨ (sub-étape 8b)** | `Container` → asset `RC_BiodiversityComposite.asset` |
| **`TechDeltaLabelBinding` ✨ (sub-étape 8b)** | `Container` → asset `RC_TechDelta.asset` |
| **`DecisionPanelBinding` ✨ (sub-étape 8c.3, refactor history)** | `Runner` → `_Bootstrap/SimulationRunner`. `Recommendation Popup` → glisse le `DecisionPopupBinding` voisin. Gère le bouton « Recommandations en cours (X) » et la list popup historique (click ligne = ré-ouvre la popup reco). |
| **`DecisionPopupBinding` ✨ (sub-étape 8c.3 post-livraison polish)** | `Runner` → `_Bootstrap/SimulationRunner`. Affiche un popup modal centré dès qu'une reco apparaît dans le journal. Met la sim en pause, slider magnitude + 3 boutons (Valider / Voir plus tard / Ignorer), reprend la sim quand la file (hors recos différées) est vide. |
| **`InitialConditionsBinding` ✨ (sub-étape 8c.4)** | `Runner` → `_Bootstrap/SimulationRunner`. Wire 3 sliders (`initial-hedgerow-density-slider`, `initial-water-table-depth-slider`, `initial-fauna-population-slider`) + bouton `initial-reset-button`. Sliders verrouillés quand `CurrentDay > 0`. |
| **`ManualActionsBinding` ✨ (sub-étape 10a)** | `Runner` → `_Bootstrap/SimulationRunner`. Câble 3 sliders + 3 boutons des « Interventions ponctuelles » du décision-panel : `manual-plant-hedges-*`, `manual-irrigation-*`, `manual-reduce-inputs-*`. Chaque clic appelle `SimulationRunner.ApplyManualXxx`, applique l'effet directement au real model (pas au shadow → TechDelta capte la divergence), pas de journal. |
| `HedgerowShaderBinding` | `Density Container` → `RC_HedgerowDensity.asset`. `Health Container` → `RC_HedgerowHealth.asset` (sub-étape 9β). `Spawn Root` → racine `Composition` enfant de `_Scene_Visual`. Scanne les enfants commençant par `hedge_`. |
| **`PondShaderBinding` ✨ (sub-étape 9α)** | `Container` → `RC_WaterTableDepth.asset`. `Spawn Root` → même racine `Composition` que HedgerowShaderBinding. Préfixe scanné par défaut : `pond`. |
| **`MeadowShaderBinding` ✨ (sub-étape 9α)** | `Container` → `RC_SoilMoisture.asset`. `Spawn Root` → même racine `Composition`. Préfixe scanné par défaut : `grass_`. |
| `SensorListBinding` | (lit les `SensorMetadataTag` posés dans la scène) |
| `ViewportWarningBinding` | (auto, lit `Screen.width`) |
| `ScenarioControlsBinding` | `Runner` → `_Bootstrap/SimulationRunner` |
| **`ScenarioPresetsBinding` ✨ (sub-étape 7c.2)** | `Runner` → `_Bootstrap/SimulationRunner` <br> `Controls Binding` → le component `ScenarioControlsBinding` sur **ce même GameObject** <br> `Presets` → tableau des 4 assets `ScenarioPreset_*.asset` (voir §5) |
| **`SpeedControlsBinding` ✨ (sub-étape 7c.3)** | `Runner` → `_Bootstrap/SimulationRunner` <br> Les 7 noms d'éléments UXML (`speed-pause-button`, `speed-x1-button`, `speed-x5-button`, `speed-x10-button`, `speed-x20-button`, `speed-skip-end-button`, `speed-day-counter`) sont laissés à leur valeur par défaut — ils correspondent aux `name=""` posés dans `Dashboard.uxml`. |

---

## 4. `_Camera`

| GameObject | Components |
|---|---|
| `MainCamera` | `Camera`, `OrthographicCameraSetup` |

---

## 5. Assets ScriptableObject de scénario (sub-étape 7c.2)

Localisation suggérée : `Assets/_Project/05_Presentation/Scenario/Presets/`

Crée le dossier s'il n'existe pas, puis : clic droit dans ce dossier →
`Create > Bocage > Scenario > Preset`. Renomme chaque asset selon la
colonne « Filename ». Les valeurs sont alignées sur les 4 scénarios
validés par `CalibrationScenarioValidationTests` (cf CALIBRATION.md).

| Filename | Id (lowercase, no spaces) | Display name | T° °C | Précip % | Hedge removal m/ha/an | Input × | MAEC % | PSE €/m/an | Horizon j |
|---|---|---|---|---|---|---|---|---|---|
| `ScenarioPreset_Reference.asset` | `reference` | Référence Perche | 0 | 0 | 0 | 1.0 | 0 | 0.00 | 365 |
| `ScenarioPreset_RCP45_2050.asset` | `rcp45` | Trajectoire RCP4.5 | 2 | -20 | 0 | 1.0 | 0 | 0.00 | 1825 |
| `ScenarioPreset_BocageBio.asset` | `bocage-bio` | Bocage vertueux MAEC | 0 | 0 | 0 | 0.5 | 100 | 1.00 | 1825 |
| `ScenarioPreset_Intensif.asset` | `intensif` | Intensif sans bocage | 0 | 0 | 5 | 2.0 | 0 | 0.00 | 1825 |

Description (champ texte, montrée en tooltip sur le bouton) :

- Référence Perche : *Baseline calibrée RICA Agreste 2024. Profit ≈ 335 €/ha/an.*
- Trajectoire RCP4.5 : *Horizon 2050, +2 °C et −20 % précipitations. Profit attendu négatif.*
- Bocage vertueux MAEC : *Bio extensif, 100 % MAEC, PSE maximal. Profit > 900 €/ha/an.*
- Intensif sans bocage : *Arrachage soutenu, intrants ×2, aucune aide environnementale.*

---

## 6. RuntimeContainers (ScriptableObjects observables)

Localisation : `Assets/_Project/Data/RuntimeContainers/`

| Asset | Producteur | Consommateurs |
|---|---|---|
| `RC_HedgerowDensity.asset` | `SimulationRunner` | `HedgerowDensityLabelBinding`, `HedgerowShaderBinding` |
| `RC_WaterTableDepth.asset` | `SimulationRunner` | `WaterTableLabelBinding`, `WaterTableDetailLabelBinding` |
| `RC_IntegratedProfitability.asset` | `SimulationRunner` | `IntegratedProfitabilityLabelBinding` |
| `RC_BiodiversityComposite.asset` ✨ | `SimulationRunner` | `BiodiversityLabelBinding` |
| `RC_TechDelta.asset` ✨ | `SimulationRunner` | `TechDeltaLabelBinding` |
| `RC_SoilMoisture.asset` ✨ (9α) | `SimulationRunner` | `MeadowShaderBinding` |
| `RC_HedgerowHealth.asset` ✨ (9β) | `SimulationRunner` | `HedgerowShaderBinding` (slot Health Container) |

(à compléter au fil des étapes — Biodiversity et TechDelta arrivent à
l'étape 8, SoilMoisture et HedgerowHealth à l'étape 9.)

---

## 7. Comment retrouver une référence cassée

Si en Play Mode un binding logge *"runner is null"* ou *"slider not found"* :

1. Ouvrir ce fichier, retrouver la ligne du binding concerné.
2. Vérifier que **tous les champs sérialisés** listés ici sont bien
   renseignés dans l'Inspector du GameObject correspondant.
3. Pour un slider/label introuvable : vérifier que le `name=""` dans
   `Dashboard.uxml` correspond bien à celui attendu dans le binding
   (champs `[SerializeField] private string ...Name`).

---

## Journal des modifications

- **2026-05-27** — Sub-étape 10b polish capteur livrée : nouveau
  composant `FaunaSensorReader` (Couche 2, pas de GameObject —
  instancié par `SimulationRunner` en Awake et reconstruit en
  lockstep dans `Rebuild`). Modifie la signature de
  `EventDetector.Detect(model, log, measuredFaunaPopulation)` —
  l'alerte fauna se base désormais sur la lecture bruitée, pas la
  vérité modèle. `HedgeChalaraEvent` retiré du détecteur — voir
  BACKLOG #16 pour la réactivation via un capteur adapté.

- **2026-05-26** — Sub-étape 10a livrée : ajout du composant
  `ManualActionsBinding` sur `_UI_Canvas` (interventions ponctuelles
  Plant / Irrigate / Reduce inputs déclenchables sans attendre un
  événement). Sémantique d'arbitrage popup formalisée dans
  DECISIONS #44 : nouvelle valeur `DecisionVerdict.Superseded` + set
  `_ignoredRecommendationTypes` côté `DecisionPopupBinding`.

- **2026-05-26** — Sub-étape 9β finalisée : `SG_hedgerow.shadergraph`
  expose la propriété `_HealthT`. Second Lerp inséré entre la sortie
  du Lerp densité et le Multiply texture, T = `1 - _HealthT` via un
  node One Minus.

- **2026-05-25** — Sub-étape 9α livrée : 2 nouveaux bindings
  (`PondShaderBinding`, `MeadowShaderBinding`) sur `_UI_Canvas`, 2
  nouveaux RCs observables (`RC_SoilMoisture`, `RC_HedgerowHealth`).
  Materials `M_Pond.mat` et `M_Meadow.mat` (shaders HLSL `S_Pond.shader`
  et `S_Meadow.shader`, cf DECISIONS #41) à affecter via le custom
  inspector de `SceneComposition_Default.asset` sur les éléments
  `pond` et `grass_border`. `HedgerowShaderBinding` étendu : nouveau
  slot `Health Container` à brancher sur `RC_HedgerowHealth.asset`.

- **2026-05-21** — Sub-étape 8c.4 livrée : panneau « Conditions
  initiales du bocage » dans le panneau gauche (`scenario-panel`,
  après Politiques publiques). 3 sliders (HedgerowDensity 0-200,
  WaterTableDepth 0.5-10, FaunaPopulation 0-1.5) + un bouton dont
  le texte est dynamique : « **Lancer la simulation** » au tout
  premier démarrage (day=0 et sim pas encore lancée), sinon
  « **Réinitialiser la simulation** ». Click applique les sliders
  via `SimulationRunner.Rebuild(...)` puis `StartTicking()` —
  expérience one-click. Édition des sliders verrouillée à
  `CurrentDay > 0`. Le shadow runner et `SpeedControlsBinding`
  s'abonnent à l'event `Rebuilt` pour resynchroniser leur état
  (modèle shadow recréé, bouton speed actif mis à jour).

- **2026-05-21** — Sub-étape 8c.3 livrée :
  `AutoActionPipeline` (pure C# Couche 3) + `AutoActionApplier`
  (MonoBehaviour Couche 5) appliquent les recos Accepted/AutoAccepted
  au real engine seul. `DecisionJournal.MarkApplied/IsApplied` pour
  garantir l'idempotence. Decision panel UI (à droite, au-dessus de la
  sensor list) liste les recos pending avec outcomes 30j/365j et
  boutons accept/reject. TechDelta KPI doit maintenant bouger quand
  l'utilisateur accepte une reco.
  + Polish post-livraison :
    (a) ordre des opérations corrigé dans `SimulationRunner.TickLoop` —
    `PublishIndicators` appelé APRÈS `TickCompleted` pour que le shadow
    soit à jour avant la lecture, fixant un drift d'un tick sur le KPI
    TechDelta ;
    (b) seuils `EventDetector` retunés pour fire sous RCP4.5 : hedge
    60→75 m/ha, drought 5→3.5 m, fauna 0.5→0.7 ;
    (c) layout complètement refondu en 2 zones conceptuelles + 1
    panneau flottant :
    **gauche = Cadre extérieur** (préréglages climat×politique +
    conditions naturelles subies + politiques publiques),
    **droite = Espace agriculteur** (décisions quotidiennes
    hedge/intrants + horizon + recommandations à arbitrer),
    **bas-droite floating = Capteurs déployés**. Les préréglages
    n'appliquent QUE les paramètres exogènes (climat + politiques +
    horizon) ; les sliders agriculteur ne sont jamais modifiés par un
    clic preset. `ScenarioPresetDefinition` allégé : suppression des
    champs `hedgeRemovalRate` et `inputIntensityFactor`. Les 4 presets
    renommés en grille climat × politique : Référence, Politique
    vertueuse, Trajectoire RCP4.5, RCP4.5 + Politique forte.

- **2026-05-21** — Sub-étape 8b livrée :
  `BiodiversityCompositeIndicator` (composite 50 % fauna + 30 % hedge +
  20 % water inverse), `TechDeltaIndicator` (% delta rentabilité real vs
  shadow), `ShadowSimulationRunner` (run parallèle avec le même seed et
  scenario partagé), 2 RCs observables + 2 LabelBindings. Hero cards
  Biodiversité et Delta tech débarrassées du tag `--deferred`. Les 5
  Hero KPIs sont maintenant tous honnêtes.

- **2026-05-21** — `OrthographicCameraSetup.viewportRect.y` passé de
  0.2222 à 0.15 pour décaler le rendu de scène vers le bas et laisser
  une respiration sous le hero-strip. Hauteur inchangée (pas de
  déformation horizontale). Réglé manuellement via l'Inspector puis
  gravé dans le défaut C# et `Main.unity`.

- **2026-05-21** — Création du document. Ajout de `ScenarioPresetsBinding`
  + 4 assets de preset (sub-étape 7c.2).
- **2026-05-21** — Sliders du panneau scénario passés en
  `show-input-field="true"` : chaque slider expose maintenant un petit
  champ numérique éditable à droite pour saisir une valeur précise (ex.
  taper `+2.3` au lieu de tenter de pointer la valeur au curseur). Drag
  reste possible pour ajuster grossièrement.
- **2026-05-21** — Le démarrage de la simulation est maintenant
  toujours en pause (`SpeedControlsBinding.Start()` force `StopTicking`
  après le `Start()` du runner). La clé PlayerPrefs
  `Bocage.Speed.LastSpeed` est toujours écrite à chaque clic d'un
  bouton play mais n'est plus relue au démarrage.
- **2026-05-21** — Ajout de `SpeedControlsBinding` et de la barre de
  vitesse top-centre (sub-étape 7c.3). 6 boutons (pause / ×1 / ×5 / ×10
  / ×20 / skip-to-end) + compteur de jours. Skip-to-end avance le
  moteur de `HorizonInDays` jours puis met en pause. La vitesse choisie
  est persistée en PlayerPrefs sous la clé `Bocage.Speed.LastSpeed`
  pour usage futur ; elle n'est PAS appliquée au démarrage — la
  simulation boote toujours en pause pour que l'utilisateur lance
  lui-même le run.

---

## Câblages prévus post-recadrage 2026-05-28 (chantiers E1-E7)

Cette section liste les câblages prévus par les chantiers E1-E7 de
`docs/ROADMAP.md`. À mettre à jour au fil des livraisons en
basculant les entrées dans la section principale ci-dessus.

### Chantier E1 — Cleanup chalara + refactor actions manuelles

Pas de nouveau câblage scène. Le `ManualActionsBinding` existant
sur `_UI_Canvas` (sub-étape 10a) sera adapté pour journaliser via
`DecisionJournal` au lieu d'appliquer directement (cf ADR #47).
Aucun champ Inspector nouveau.

### Chantier E2 — Saisonnalité + WeatherStation

**Nouveau GameObject** :

| GameObject enfant | Components | Références à brancher |
|---|---|---|
| `_Bootstrap/SimulationRunner` (extension) | + champ `SeasonalWeatherDataAsset` | → asset `SeasonalWeatherData_Mortagne.asset` (Couche 01, dossier `Assets/_Project/Data/Weather/`). |

**Nouveau composant sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| `MonthSelectorBinding` (E2) | `Runner` → `_Bootstrap/SimulationRunner`. Combo UXML par défaut `initial-month-combo` dans section « Conditions initiales » du scenario panel. |

**Nouveau RC** :

| Asset | Producteur | Consommateurs |
|---|---|---|
| (aucun nouveau RC — la saisonnalité est consommée via `EcosystemModel.CurrentWeather` existant, étendu) | — | — |

### Chantier E3 — Carbone sol + EddyTower

**Nouveau composant sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| (Pas de binding dédié — les 2 sliders « Couverts d'interculture » et « Restitution résidus » sont ajoutés au scenario panel UXML existant, câblés via le `ScenarioControlsBinding` actuel élargi.) | — |

**Nouveau RC** :

| Asset | Producteur | Consommateurs |
|---|---|---|
| `RC_SoilCarbonStock.asset` | `SimulationRunner` | `OngletClimatBinding` (E6), `SensorInspectorPanelBinding` (E6, mode EddyTower). |

### Chantier E4 — Faune visible 4 espèces

**Nouveau GameObject** :

| GameObject | Components | Références à brancher |
|---|---|---|
| `_Scene_Visual/Fauna` | `FaunaPool` (composant Couche 05) | `Placement Definition` → asset `FaunaPlacement_Default.asset`. `Spawn Root` → ce GameObject lui-même (ou enfant). `Random Seed Source` → seed maître du `SimulationRunner`. |
| Chaque pool member (pré-instancié au Awake par `FaunaPool`) | `SpriteRenderer`, `FaunaIdleMotion` | Paramètres lus depuis `FaunaSpeciesDefinition`. |

**Nouveau composant sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| `FaunaPoolBinding` (E4) | `Pool` → `_Scene_Visual/Fauna` (GameObject portant `FaunaPool`). `Biodiv Container` → `RC_BiodiversityComposite.asset`. `Habitat / Water / Inputs Factor Containers` → `RC_FaunaFactor{Habitat,Water,Inputs}.asset` (après E5). |

**Assets ScriptableObject** (dans `Assets/_Project/Data/Fauna/`) :

| Asset | Notes |
|---|---|
| `FaunaSpecies_Heron.asset` | Sprite frames héron, seuil apparition élevé (espèce sensible). |
| `FaunaSpecies_Owl.asset` | Sprite frames chouette, position perchée, pas d'animation. |
| `FaunaSpecies_Harrier.asset` | Sprite frames busard, oscillation horizontale lente. |
| `FaunaSpecies_Swallow.asset` | Sprite frames hirondelle, oscillation horizontale lente. |
| `FaunaPlacement_Default.asset` | SO racine listant les 4 espèces ci-dessus. |

### Chantier E5 — Capital + biodiv 3 facteurs

**Nouveaux RC** :

| Asset | Producteur | Consommateurs |
|---|---|---|
| `RC_FaunaFactorHabitat.asset` | `SimulationRunner` | `OngletBiodivBinding`, `FaunaPoolBinding` (E4 finalisé). |
| `RC_FaunaFactorWater.asset` | `SimulationRunner` | idem. |
| `RC_FaunaFactorInputs.asset` | `SimulationRunner` | idem. |
| `RC_TotalInvestment.asset` | `SimulationRunner` | `OngletEconomieBinding`, `DecisionPopupBinding` (affichage popup). |
| `RC_InvestmentHorizon.asset` | `SimulationRunner` | idem. |

Pas de nouveau GameObject. Les nouveaux RC sont câblés en producteur
via `SimulationRunner` existant (slots de publication à étendre).

### Chantier E6 — Panneau inspection capteurs + 3 onglets Niveau B

**Configuration scène** (modifications scène `Main.unity`) :

- Ajout de `Physics2DRaycaster` sur `_Camera/MainCamera` (composant
  URP indispensable pour click sprites 2D).
- Ajout d'un `BoxCollider2D` (size matchant le sprite) sur chacun
  des 5 GameObjects capteurs visibles dans `_Scene_Visual` (sprites
  `weather_station`, `piezometer`, `acoustic_sensor`, `photo_trap`,
  `eddy_covariance_tower`).
- Composant `SensorClickHandler` sur chacun des 5 sprites : publie
  un event dans `SensorClickedEventBus` (statique, Couche 05) avec
  le type de capteur cliqué.
- L'EventSystem Unity doit être actif dans la scène (déjà en place
  pour UI Toolkit ; vérifier).

**Nouveaux composants sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| `SensorInspectorPanelBinding` (E6) | `Runner` → `_Bootstrap/SimulationRunner`. `Seasonal Weather Data` → asset `SeasonalWeatherData_Mortagne.asset` (pour normales mensuelles affichées en mode WeatherStation). S'abonne à `SensorClickedEventBus` pour ouvrir/configurer le panneau. |
| `WeatherNormalsPanelBinding` (E6) | (sous-panneau du précédent — peut être merged dans `SensorInspectorPanelBinding` selon la complexité, à arbitrer à l'implémentation). |
| `OngletBiodivBinding` (E6) | Containers : `RC_BiodiversityComposite`, `RC_FaunaFactorHabitat`, `RC_FaunaFactorWater`, `RC_FaunaFactorInputs`. `Pool` → `_Scene_Visual/Fauna` (pour comptage espèces visibles). |
| `OngletClimatBinding` (E6) | `Runner` → `_Bootstrap/SimulationRunner` (lecture `WeatherStationReader` history + `EddyTowerSensorReader` history via sliding window). Containers : `RC_WaterTableDepth`, `RC_SoilCarbonStock`. |
| `OngletEconomieBinding` (E6) | `Runner` → `_Bootstrap/SimulationRunner` (lecture `journal.TotalInvestment` + indicateurs). Containers : `RC_IntegratedProfitability`, `RC_TotalInvestment`, `RC_InvestmentHorizon`. |

**UXML / USS** :

- `Assets/_Project/05_Presentation/UI/SensorInspectorPanel.uxml` +
  `.uss` : panneau modal réutilisable, 5 layouts par capteur.
- `Dashboard.uxml` : 3 onglets Niveau B existants enrichis (lignes
  supplémentaires).

### Chantier E7 — Polish + publication

Pas de nouveau câblage scène. Configuration build only (Crunch
DXT5 conditionnel sur sprites lourds, cf `docs/ASSETS_LIST.md` §6
étape 7).

---

## Mémo synthèse — nouveaux composants sur `_UI_Canvas` post-recadrage

| Component | Chantier | ADR cadrant |
|---|---|---|
| `MonthSelectorBinding` | E2 | #52 |
| `FaunaPoolBinding` | E4 | #49 |
| `SensorInspectorPanelBinding` | E6 | #53 |
| `WeatherNormalsPanelBinding` (optionnel, sub-panneau) | E6 | #53 |
| `OngletBiodivBinding` | E6 | #54 |
| `OngletClimatBinding` | E6 | #54 |
| `OngletEconomieBinding` | E6 | #54 |

Au total : 6-7 nouveaux components sur `_UI_Canvas` à l'issue de la
roadmap E1-E7. Aucune restructuration de la hiérarchie scène (les
7 racines préfixées `_` restent inchangées, conformes `CLAUDE.md`
§8).
