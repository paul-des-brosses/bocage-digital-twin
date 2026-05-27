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
