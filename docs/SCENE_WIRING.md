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
| `SimulationRunner` | `SimulationRunner` (Couche 5 Presentation) | RCs : HedgerowDensity, WaterTable, IntegratedProfitability, **BiodiversityComposite ✨**, **TechDelta ✨**. `Shadow Runner` → glisser le GO ci-dessous. |
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
| **`DecisionPanelBinding` ✨ (sub-étape 8c.3)** | `Runner` → `_Bootstrap/SimulationRunner`. Spawn une carte par reco pending, boutons accept/reject mutent le journal directement. |
| `HedgerowShaderBinding` | (lié à la composition de scène) |
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

(à compléter au fil des étapes — Biodiversity et TechDelta arrivent à
l'étape 8.)

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
