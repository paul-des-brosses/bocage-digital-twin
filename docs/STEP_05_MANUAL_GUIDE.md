# Étape 5 — Manual guide

Tout le code C# est en place. Il reste 7 actions manuelles dans Unity,
dans cet ordre.

> **Avant de commencer** : ouvre Unity sur ce projet, attends que la
> compilation soit verte (Console sans erreur rouge). Si la Console
> râle sur un namespace `Bocage.Data.RuntimeContainers` introuvable,
> Unity n'a pas encore recompilé après l'ajout d'asmdef — laisse-le
> finir, ou clic droit sur le dossier `_Project/` → `Reimport`.

---

## 1. Créer l'asset `RC_HedgerowDensity`

**Où** : Project window → clic droit sur `Assets/_Project/Data/RuntimeContainers/`.

**Action** :

- `Create > Bocage > Data > RC_HedgerowDensity`
- Nomme-le exactement `RC_HedgerowDensity` (sans suffixe).
- Garde les valeurs par défaut (0 / 0). Le runner écrira la vraie valeur
  au boot.

**Vérification** : tu vois `RC_HedgerowDensity.asset` à côté du `.cs`,
avec deux champs visibles dans l'Inspector : `Meters Per Hectare` et
`Normalized01`.

---

## 2. Ajouter le `SimulationRunner` à `_Bootstrap`

**Où** : Hierarchy → `_Bootstrap`.

**Action** :

1. Sélectionne `_Bootstrap`.
2. `Add Component` → cherche **Simulation Runner**.
3. Dans l'Inspector du Runner :
   - **Master Seed** : `1` (laisse).
   - **Ticks Per Second** : `1` pour démarrer (passe à `10` ou `20`
     temporairement si tu veux voir bouger plus vite pendant la
     validation — la croissance des haies est de ~0.5 m/ha par an, donc
     à x1 le chiffre bouge lentement).
   - **Auto Start** : coché.
   - **Hedgerow Density Container** : drag `RC_HedgerowDensity.asset`
     dans ce slot.

**Vérification** : au lancement de Play Mode, la Console affiche :
```
[Sim] [SimulationRunner] engine built seed=1 initialHedgerowDensity=90.0 m/ha
```

---

## 3. Créer le PanelSettings pour UI Toolkit

UI Toolkit a besoin d'un `PanelSettings` qui décrit la résolution de
référence, le scale mode, etc.

**Où** : `Assets/_Project/05_Presentation/UI/`.

**Action** :

- Clic droit dans le dossier `UI/` → `Create > UI Toolkit > Panel
  Settings Asset`.
- Nomme-le `PanelSettings_Dashboard`.
- Inspector → **Scale Mode** : `Scale With Screen Size`, **Reference
  Resolution** : `1920 x 1080`.
- **Screen Match Mode** : si l'option `Match Width Or Height` est
  disponible, choisis-la et mets **Match** à `0.5`. Sinon laisse la
  valeur par défaut, pas critique à ce stade.
- **Sort Order** : si ce champ existe, mets `100`. S'il n'existe pas
  dans ta version Unity, laisse tomber (utile seulement quand plusieurs
  `UIDocument` se superposent).

---

## 4. Créer le GameObject `_UI_Canvas` avec UIDocument

**Où** : Hierarchy.

**Action** :

1. Vérifie que la racine `_UI_Canvas` existe déjà (à 7 racines préfixées
   `_`, cf §8 CLAUDE.md). Si vide, c'est normal.
2. Sélectionne `_UI_Canvas` → `Add Component > UI > UI Document` (le
   moteur de UI Toolkit, à ne pas confondre avec Canvas uGUI).
3. Dans l'Inspector du `UIDocument` :
   - **Panel Settings** : drag `PanelSettings_Dashboard`.
   - **Source Asset** : drag `Dashboard.uxml`.
4. `Add Component` → cherche **Hedgerow Density Label Binding**.
5. Dans l'Inspector du Binding :
   - **Container** : drag `RC_HedgerowDensity.asset`.
   - **Label Name** : `hedgerow-density-value` (laisse).

**Vérification** : Play Mode → tu vois en haut à gauche un cartouche
sombre avec « Densité de haies » en label italique crème et un grand
chiffre proche de `90.0 m/ha`. Le chiffre bouge très lentement à x1.

> **Astuce de validation rapide** : passe **Ticks Per Second** du
> Runner à `20`, lance Play, tu vois le chiffre changer plusieurs fois
> en quelques secondes. Remets à `1` après vérification.

---

## 5. Créer le Shader Graph `SG_Hedgerow`

Même démarche qu'à l'Étape 4 pour `SG_Sky`. On crée un shader qui tinte
le sprite par interpolation entre deux couleurs, modulée par la densité
normalisée.

**Où** : `Assets/_Project/05_Presentation/Scene/Shaders/`.

**Action** :

1. Clic droit → `Create > Shader Graph > URP > Sprite Unlit Shader
   Graph`. Nomme-le `SG_Hedgerow`.
2. Double-clique pour l'ouvrir.
3. **Blackboard** (panneau de gauche) → `+` :
   - `Color` nommée `_SparseColor`. Default : un vert pâle légèrement
     jaune (~ RGB 152, 168, 110).
   - `Color` nommée `_DenseColor`. Default : un vert profond (~ RGB 64,
     94, 56).
   - `Float` nommé `_Density`. **Mode** : `Slider`, **Min** : `0`,
     **Max** : `1`, **Default** : `0.5`.
   - `Texture2D` nommée `_MainTex`. Default : laisse vide.
4. **Graph view** (la grande zone) :
   - Drag les 4 properties depuis le Blackboard.
   - Crée un node `Lerp` (clic droit → `Create Node` → recherche
     "lerp").
   - Crée un node `Sample Texture 2D`.
   - Crée un node `Multiply`.
   - Wires :
     - `_SparseColor` → **Lerp.A**
     - `_DenseColor` → **Lerp.B**
     - `_Density` → **Lerp.T**
     - `_MainTex` → **Sample Texture 2D.Texture** (laisse UV et Sampler
       vides — UV0 par défaut)
     - `Lerp.Out` → **Multiply.A**
     - `Sample Texture 2D.RGBA(4)` → **Multiply.B**
     - `Multiply.Out` → **Fragment.Base Color**
     - `Sample Texture 2D.A(1)` → **Fragment.Alpha**
5. **Save Asset** (bouton en haut à gauche du graph editor).

> **Pourquoi le Multiply** : sans lui, le Lerp remplace toute la
> couleur du sprite par une teinte uniforme et on perd le détail
> visuel (branches, feuillage). Multiplier par les RGB de la texture
> conserve la forme et l'ombrage tout en colorant.
>
> **Pourquoi `_MainTex`** : sans texture en entrée, le shader peint un
> aplat sur le rectangle du sprite et ignore la silhouette. En
> échantillonnant la texture et en utilisant son alpha sur le Fragment,
> on conserve la forme.

---

## 6. Créer le matériau `M_Hedgerow`

**Où** : `Assets/_Project/05_Presentation/Scene/Materials/`.

**Action** :

1. Clic droit → `Create > Material`. Nomme-le `M_Hedgerow`.
2. Inspector → dropdown **Shader** → tape "SG_Hedgerow" dans la barre
   de recherche → sélectionne `Shader Graphs/SG_Hedgerow`.
3. Laisse les valeurs par défaut. Le `_Density` sera écrasé par le
   binding au runtime ; `_MainTex` sera fourni par chaque SpriteRenderer
   automatiquement quand `M_Hedgerow` sera assigné.

---

## 7. Appliquer `M_Hedgerow`, capturer la composition, brancher le binding

⚠️ **Point clé** : `SceneAssembler` détruit et recrée les sprites à
chaque Play. L'assignation de matériau et toute référence directe à
ces sprites doivent donc passer par la composition data-driven (le
ScriptableObject `SceneComposition_Default`), pas par une assignation
volatile dans la scène.

**Étape 7a — Assigner le matériau aux sprites de haies (et au pollard
standalone)** :

1. Hierarchy → déplie `_Scene_Visual` (ou le spawnRoot du
   `SceneAssembler` si tu en as défini un explicite).
2. Sélectionne chaque sprite de haie (`hedge_high_pollard_01`,
   `hedge_high_no_tree`, `hedge_low_01`, et toutes leurs variantes que
   tu as ajoutées). Pour chacun :
   - Inspector → composant `Sprite Renderer` → champ **Material** →
     drag `M_Hedgerow`.
3. Fais pareil pour `pollard_ash_main` (l'arbre têtard standalone). Il
   est intégré au système bocager : sa couleur doit suivre la même
   variable de densité, sans quoi on a un arbre tinté à côté d'un autre
   non tinté → incohérence visuelle.

**Étape 7b — Capturer dans le ScriptableObject** :

1. Sélectionne le GameObject qui porte le composant `SceneAssembler`
   (probablement `_Scene_Visual` lui-même).
2. Inspector → bouton **Capture Scene → Composition**.
3. Console : tu dois voir `[SceneAssemblerEditor] captured N elements
   into SceneComposition_Default`.
4. Vérifie : sélectionne `SceneComposition_Default.asset`, déplie un
   élément haie → le champ **Material** doit afficher `M_Hedgerow`.

**Étape 7c — Ajouter `HedgerowShaderBinding`** :

1. Sélectionne `_Scene_Visual` (ou un GameObject de bindings de ton
   choix sous `_Scene_Visual`).
2. `Add Component` → cherche **Hedgerow Shader Binding**.
3. Configure :
   - **Container** : drag `RC_HedgerowDensity.asset`.
   - **Spawn Root** : drag le même transform que celui utilisé par
     `SceneAssembler` (souvent `_Scene_Visual`).
   - **Hedge Name Prefixes** : Size = `2`, Element 0 = `hedge_`,
     Element 1 = `pollard_`. Tout enfant du spawnRoot dont le nom
     commence par un de ces préfixes sera tinté.
   - **Density Property** : `_Density` (laisse).

**Vérification finale** : Play. Console doit afficher :
```
[Debug] [HedgerowShaderBinding] discovered N hedge renderers under <SpawnRoot>
[Sim] [SimulationRunner] engine built seed=1 initialHedgerowDensity=90.0 m/ha
```
- KPI texte en haut à gauche affiche `90.0` (ou proche) et dérive lentement.
- Haies et pollard ont une teinte verte intermédiaire entre les deux
  couleurs du shader, modulée par la densité.

**Test rapide de la chaîne de tint** :

1. Stop Play.
2. Sélectionne `M_Hedgerow`, déplace son slider `_Density` de 0 à 1.
3. En Scene view (Edit Mode), les haies passent du vert pâle au vert
   profond. Ce test bypasse le binding et valide shader + matériau +
   sprites.

⚠️ Modifier `Normalized01` directement dans l'Inspector de
`RC_HedgerowDensity.asset` ne déclenchera pas `OnChanged` (mutation
hors `Set()`), donc rien ne bouge à l'écran. C'est normal — la
notification ne passe que via le `SimulationRunner` qui appelle
`Set()` à chaque tick.

---

## Critère de validation de l'Étape 5

- ✅ Le KPI texte s'affiche en haut à gauche et la valeur évolue tick
  par tick.
- ✅ Les haies (et le pollard) à l'écran réagissent visuellement à la
  densité du modèle.
- ✅ Console : aucune erreur rouge, aucun warning « container not
  assigned ».
- ✅ Build WebGL toujours fonctionnel (à vérifier en fin d'étape).
- ✅ Tests EditMode passent (Window → General → Test Runner → EditMode
  → Run All ; les 6 tests `HedgerowDensityIndicatorTests` doivent
  passer en plus des tests existants).
