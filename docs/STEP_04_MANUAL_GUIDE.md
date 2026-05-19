# Étape 4 — Guide manuel Unity

Ce document liste les actions à exécuter dans l'éditeur Unity pour finaliser
l'Étape 4. Les fichiers de code, les sorting layers et la documentation
sont déjà livrés par Claude Code.

**Ordre d'exécution recommandé** : 1 → 2 → 3 → 4 → 5 → 6 → 7.
Comptez ~45 minutes la première fois.

---

## 1. Ouvrir Unity et laisser les `.meta` se générer

**Action** : ouvrir le projet dans Unity 6 LTS. Laisser l'éditeur
recompiler. Quand la console est verte (aucune erreur), passer à la
suite.

**Pourquoi pas automatisable** : les fichiers `.meta` contiennent des
GUIDs uniques que seule l'éditeur peut générer de manière fiable.

**Vérification** : dans `Assets/_Project/05_Presentation/Scene/`, les
trois nouveaux dossiers `Composition/`, `Sky/`, `CameraRig/` apparaissent
avec leurs scripts. Aucun warning de compilation rouge.

**Piège courant** : si Unity n'avait pas le focus, le refresh n'a pas
eu lieu. `Assets > Refresh` (Ctrl+R) force la prise en compte.

---

## 2. Vérifier les sorting layers

**Action** : `Edit > Project Settings > Tags and Layers > Sorting Layers`.

**Résultat attendu** : 8 layers dans cet ordre exact (de haut en bas dans
l'inspecteur, ce qui correspond à l'ordre de rendu, fond → avant) :

```
Default
Sky
Background
Midground
Foreground
Sensors
Fauna
FX
```

**Pourquoi pas automatisable, en fait si** : le fichier `TagManager.asset`
a déjà été édité par Claude. Cette étape est un simple contrôle visuel.

**Si les layers manquent** : Unity n'a pas relu `ProjectSettings/TagManager.asset`.
Quitter et rouvrir l'éditeur.

---

## 3. Créer le Shader Graph `SG_Sky`

### 3.1 Création de l'asset

**Action** : dans le Project, naviguer à
`Assets/_Project/05_Presentation/Scene/Shaders/`. Clic droit →
`Create > Shader Graph > URP > Unlit Shader Graph`.
Renommer en **`SG_Sky`**.

### 3.2 Configurer les propriétés exposées (Blackboard)

Ouvrir `SG_Sky` (double-clic). Dans le panneau **Blackboard** (en haut à
gauche), ajouter exactement 3 propriétés avec ces noms exposés (le **Reference**
doit être précis, c'est ce que `SkyController.cs` lit) :

| Display Name | Type  | Reference     | Default          |
| ------------ | ----- | ------------- | ---------------- |
| Top Color    | Color | `_TopColor`   | RGB ≈ 41,51,72   |
| Bottom Color | Color | `_BottomColor`| RGB ≈ 217,191,140|
| Horizon      | Float | `_Horizon`    | 0.55 (mode Slider 0→1)|

Pour chaque propriété : sélectionner la propriété dans le Blackboard, dans
le **Node Settings** (panneau de droite), cocher `Exposed` et renseigner
exactement le champ `Reference`.

### 3.3 Construire le graphe

Le graphe à câbler (du plus à gauche au plus à droite) :

```
[UV (node Sample → "UV")]
        │
        ├─── (sortir le composant Y) ──┐
                                       │
[Horizon (property)] ──────────────────┤
                                       ▼
                              [Smoothstep]   (Edge1 = Horizon − 0.15,
                                       │     Edge2 = Horizon + 0.15,
                                       │     In    = UV.y)
                                       ▼
                                     [Lerp]   (A = Top Color,
                                       │     B = Bottom Color,
                                       │     T = smoothstep output)
                                       ▼
                          [Fragment.Base Color]
```

Étapes détaillées :

1. **Right-click → Create Node → Input > Geometry > UV**. Glisser en place.
2. **Right-click → Create Node → Channel > Split**. Connecter `UV.Out → Split.In`.
   On va utiliser le port `G` (= composant Y de l'UV).
3. Glisser la propriété **Horizon** depuis le Blackboard dans le graphe.
4. **Right-click → Create Node → Math > Basic > Subtract**. Brancher
   `Horizon → A`, mettre `B = 0.15`. C'est le `Edge1` du smoothstep.
5. **Right-click → Create Node → Math > Basic > Add**. Brancher
   `Horizon → A`, mettre `B = 0.15`. C'est le `Edge2` du smoothstep.
6. **Right-click → Create Node → Math > Interpolation > Smoothstep**.
   Connecter `Subtract.Out → Edge1`, `Add.Out → Edge2`, `Split.G → In`.
7. Glisser les propriétés **Top Color** et **Bottom Color** dans le graphe.
8. **Right-click → Create Node → Math > Interpolation > Lerp**. Connecter
   `Top Color → A`, `Bottom Color → B`, `Smoothstep.Out → T`.
9. Connecter `Lerp.Out` au port **Base Color** du **Fragment** (le bloc
   à droite de l'écran).

### 3.4 Vérifier et sauver

- Le panneau **Main Preview** (bas droit) doit montrer un dégradé
  vertical sombre→clair.
- Cliquer **Save Asset** (en haut à gauche du Shader Graph).
- Fermer le Shader Graph.

**Pourquoi pas automatisable** : un fichier `.shadergraph` est du YAML
auto-généré truffé de GUIDs internes que je ne peux pas écrire de
manière fiable. C'est strictement une opération éditeur.

---

## 4. Créer le matériau et le quad ciel

### 4.1 Matériau

**Action** : dans `Assets/_Project/05_Presentation/Scene/Materials/`,
clic droit → `Create > Material`. Renommer **`M_Sky`**. Dans
l'inspecteur, champ **Shader**, sélectionner `Shader Graphs / SG_Sky`.
Les 3 paramètres (Top Color, Bottom Color, Horizon) doivent apparaître.
Laisser les valeurs par défaut, ils seront poussés par `SkyController`.

### 4.2 Quad dans la scène

**Action** : ouvrir `Assets/_Project/Main.unity`. Dans `_Scene_Visual`,
clic droit → `3D Object > Quad`. Renommer **`Sky`**.

- **Transform** : Position = (0, 0, 10), Rotation = (0,0,0),
  Scale = (40, 25, 1) — le quad couvre largement le champ orthographique.
- **Mesh Renderer** : appliquer `M_Sky` dans le slot **Material**.
- Sur le même `Sky` GameObject : `Add Component > Sky Controller`
  (le script qu'on a livré). Glisser le **Mesh Renderer** du Quad dans le
  slot `Target Renderer`.

**Vérification** : en Scene View, le quad montre un dégradé. Modifier
`Top Color` dans `SkyController` met à jour le dégradé en live (grâce au
`OnValidate`).

**Piège courant** : le quad doit être **derrière** tout le reste. Si
besoin, ajuster son Z à une valeur positive plus élevée (le quad est
plus loin de la caméra), mais 10 suffit avec une caméra à Z=-10 et une
ortho size de 5.

---

## 5. Caméra orthographique

**Action** : sélectionner `_Camera > Main Camera` (ou créer la Camera
si elle n'existe pas via `Camera` dans le Hierarchy).

- `Add Component > Orthographic Camera Setup`.
- Laisser les valeurs par défaut (ortho size = 5, Z = -10, fond
  anthracite).

**Vérification au Play** : la console affiche `[Debug] [Camera] orthographic ok size=5`.

**Pourquoi un script et pas la config inspector seule** : le script
réimpose les valeurs au Awake, donc même si on touche par erreur la
caméra en éditeur, le runtime reste déterministe (cf DECISIONS.md #16).

---

## 6. Créer la `SceneCompositionDefinition` et l'assembler

### 6.1 Asset de composition

**Action** : dans `Assets/_Project/05_Presentation/Scene/Composition/`,
clic droit → `Create > Bocage > Scene > Composition Definition`.
Le fichier se nomme par défaut `SceneComposition_Default.asset`,
garder ce nom.

### 6.2 Remplir les éléments

Sélectionner l'asset. Dans l'inspecteur, ouvrir la liste **Elements**
et ajouter une entrée par sprite à composer. Proposition de composition
de référence (à ajuster visuellement) :

| id                  | sprite                          | worldPosition | scale | sortingLayerName | sortingOrderInLayer |
| ------------------- | ------------------------------- | ------------- | ----- | ---------------- | ------------------- |
| hills               | `hills_perche`                  | (0, 0.5)      | 1.0   | Background       | 0                   |
| hedge_high_pollard_01 | `hedge_high_pollard_01`       | (-3, -0.5)    | 1.0   | Midground        | 0                   |
| hedge_high_no_tree  | `hedge_high_no_tree`            | (3, -0.5)     | 1.0   | Midground        | 1                   |
| hedge_low_01        | `hedge_low_01`                  | (-1, -1)      | 0.9   | Midground        | 2                   |
| pond                | `pond`                          | (1.5, -2)     | 1.0   | Foreground       | 0                   |
| pollard_ash_main    | `pollard_ash_main`              | (-2.5, -1.8)  | 1.1   | Foreground       | 1                   |
| grass_border        | `grass_border`                  | (0, -3)       | 1.0   | Foreground       | 2                   |

**Notes** :

- Les positions et scales sont des valeurs de départ raisonnables. Tu
  ajusteras visuellement en Play Mode.
- Tu peux dupliquer une entrée et toggler `flipX` pour réutiliser une
  variante de haie.
- Les sprites sont chargés par drag-and-drop depuis `Scene/Sprites/<catégorie>/`.

### 6.3 Brancher l'assembler

**Action** : dans `Main.unity`, sélectionner `_Scene_Visual`. Si la
racine n'a pas encore le composant, `Add Component > Scene Assembler`.
Glisser `SceneComposition_Default` dans le slot **Composition**.
Laisser **Spawn Root** vide (le script tombe sur `_Scene_Visual` par
défaut).

**Vérification au Play** : la scène se peuple des 7 sprites, console :
`[Debug] [SceneAssembler] composed 7 elements from SceneComposition_Default`.

**Piège courant** : si un sprite n'apparaît pas → vérifier dans
l'inspecteur du sprite que **Pixels Per Unit** est cohérent (100 par
défaut), **Sprite Mode = Single**, **Filter Mode = Point (no filter)**
ou **Bilinear** selon le rendu désiré.

---

## 7. Vérifier le build WebGL

**Action** : `File > Build Settings > Build`. Sortie dans
`Builds/WebGL_step04/`.

**Critère de validation Étape 4** (cf ROADMAP.md) :

- ✅ Scène lisible en Play Mode (dégradé ciel + collines + haies +
  prairie + mare).
- ✅ Console : `[Camera] orthographic ok` puis `[SceneAssembler] composed N elements`.
- ✅ Build WebGL passe sans erreur.
- ⏳ Polish visuel (positions, scales, couleurs ciel) — itère à ton
  goût avant de clôturer l'étape.

---

## 8. Commit

Quand tout est vert, signaler à Claude Code qui ajoutera les fichiers
`.meta` générés par Unity et commitera :

- `chore(repo): import meta files for Étape 4 Presentation scaffolding`
- `feat(presentation): Étape 4 — data-driven scene composition + SG_Sky`

---

## Annexe — Diagnostic rapide

| Symptôme                                       | Cause probable                                                                 |
| ---------------------------------------------- | ------------------------------------------------------------------------------ |
| Aucun log `[Camera]` au Play                   | `OrthographicCameraSetup` pas attaché à la Main Camera                         |
| Aucun log `[SceneAssembler]`                   | Composant `SceneAssembler` pas sur `_Scene_Visual` ou Composition non assignée |
| Sprites empilés au mauvais ordre               | Mauvais `sortingLayerName` dans l'asset Composition (vérifier l'orthographe)   |
| Ciel uniforme noir                             | `Target Renderer` du `SkyController` non assigné                               |
| Erreur shader « property `_TopColor` not found » | Les `Reference` du Blackboard ne correspondent pas — vérifier §3.2 exactement  |
| Quad ciel devant les sprites                   | Z du quad trop bas, le mettre à Z=10 (ou plus loin)                            |
