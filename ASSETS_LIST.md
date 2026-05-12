# ASSETS_LIST.md — Liste des assets visuels

Inventaire exhaustif des assets nécessaires au projet. Statut à mettre
à jour au fil de la production : `à générer` → `généré` → `post-traité`
→ `intégré`.

---

## 1. Sprites scène

### Background

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hills_perche.png` | Nanobanana | généré (pipeline en attente) | Paysage de collines vallonnées du Perche, 5 couches d'atmospheric perspective. Source détourée prête (3168×1344, alpha 32 bits). **Surveillance quantization** : préserver l'ordre tonal des crêtes (plus pâle au lointain, plus saturé au proche), une inversion ferait s'effondrer la profondeur. |

### Midground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hedge_low_01.png` | Nanobanana | généré (pipeline en attente) | Haie basse, variante 1. Source détourée prête. |
| `hedge_low_02.png` | Nanobanana | généré (pipeline en attente) | Haie basse, variante 2 (variation visuelle). Source détourée prête. |
| `hedge_low_03.png` | Nanobanana | généré (pipeline en attente) | **Variante DA ajoutée** (vs liste initiale qui n'en prévoyait que 2) pour enrichir la diversité visuelle quand le sprite sera tilé en scène. |
| `hedge_high_pollard_01.png` | Nanobanana | généré (pipeline en attente) | Haie haute avec arbre têtard, variante 1. Source détourée prête. |
| `hedge_high_pollard_02.png` | Nanobanana | généré (pipeline en attente) | Haie haute avec arbre têtard, variante 2. Source détourée prête. |
| `hedge_high_no_tree.png` | Nanobanana | généré (pipeline en attente) | **Sprite ajouté DA** (non prévu liste initiale). Comble le linéaire de haie haute entre deux pollards (en bocage réel, pollards espacés tous les 8-15 m — éviter l'effet « pollard tous les 2 m » irréaliste lors du tiling). |
| `hedge_thin_sparse_01.png` | Nanobanana | généré (pipeline en attente) | Haie en état **modérément dégradé** (haie encore continue, ~30 % moins dense que la saine, 1-2 troncs nus visibles), variante 1. Générée avec `hedge_low_01` comme seconde image de référence ip-adapter pour préserver la cohérence « même haie en moins bon état ». **Note sémantique** : malgré le nom de fichier `sparse`, ce n'est pas une dégradation extrême (premiers essais générant 3-5 fragments séparés ont été rejetés et archivés ailleurs). |
| `hedge_thin_sparse_02.png` | Nanobanana | généré (pipeline en attente) | Idem variante 2 (alignée sur `hedge_low_02`). |
| `hedge_thin_sparse_03.png` | Nanobanana | généré (pipeline en attente) | Idem variante 3 (alignée sur `hedge_low_03`). |

### Foreground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `pollard_ash_main.png` | Nanobanana | généré (pipeline en attente) | Arbre têtard de frêne, élément iconique, premier plan. Source détourée prête. **Note résolution** : sortie en 1024×651, sensiblement plus petite que les autres sprites foreground (~2.5-2.8 K de large). Pas bloquant — `postprocess.py` resize au longest-side cible — mais à confirmer côté DA si le détail est suffisant à l'échelle scène finale. |
| `pond.png` | Nanobanana | généré (pipeline en attente) | Mare avec bords, sprite avec zone d'eau modulable par shader. Source détourée prête (2816×1536, alpha 32 bits). |
| `grass_border.png` | Nanobanana | généré (pipeline en attente) | Bordure de prairie premier plan. Source détourée prête (2568×1632, alpha 32 bits). |

### Fauna

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `bird_swallow_flight.png` | Nanobanana | intégré | Hirondelle en vol, sprite simple animable. Validation DA 2026-04-26 (option 1 — palette `v0.1-provisional` retenue sans rééquilibrage). Sprite final : `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/swallow.png` (256×121). |
| `bird_owl_flight.png` | Nanobanana | généré (pipeline en attente) | Chouette chevêche en vol. Source détourée prête (2848×1490, alpha 32 bits). |
| `bird_harrier_flight.png` | Nanobanana | généré (pipeline en attente) | Busard Saint-Martin en glide. Source détourée prête (2568×1632, alpha 32 bits). Introduit un gris-bleu pâle dans la palette corpus. |
| `heron_static.png` | Nanobanana | généré (pipeline en attente) | Héron cendré au bord de la mare, pose de chasse statique. Source détourée prête (2568×1632, alpha 32 bits). Introduit un gris-bleu froid moyen et un ocre chaud (pattes) dans la palette corpus. |
| `amphibian_small.png` | Nanobanana | **non produit, écarté** | Sacrifié comme prévu (statut optionnel/coupable). La lecture biodiversité est portée par les 4 autres sprites faune (hirondelle, chouette, busard, héron). Décision DA 2026-05-12. |

### Sensors (visibles dans la scène)

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `weather_station.png` | Nanobanana | généré (pipeline en attente) | Station météo, mât avec capteurs. Source détourée prête (2568×1632, alpha 32 bits). Bouclier solaire en blanc cassé pâle. |
| `piezometer.png` | Nanobanana | généré (pipeline en attente) | Piézomètre, tube de mesure de nappe. Source détourée prête (2572×1632, alpha 32 bits). **Surveillance quantization** : léger modelé 3D résiduel sur le tube ; la quantization doit l'écraser. Si elle ne le fait pas et que le modelé persiste de façon visible, retouche manuelle. |
| `acoustic_sensor.png` | Nanobanana | généré (pipeline en attente) | Capteur acoustique, micro directionnel. Source détourée prête (1023×651, alpha 32 bits). **Note résolution** : sortie en 1023×651, sensiblement plus petite que les autres capteurs (~2.5 K de large). Cf. note `pollard_ash_main`. |
| `photo_trap.png` | Nanobanana | généré (pipeline en attente) | Piège photo, **sprite standalone sans support** (écart vs. spec initiale « boîtier sur tronc »). Choix DA assumé pour : (a) éviter conflit visuel avec troncs existants, (b) flexibilité de placement à l'intégration, (c) cohérence avec les autres capteurs standalone. À l'intégration Unity, positionner sur un élément porteur (tronc de pollard ou piquet implicite). Source détourée prête (2043×1644 après crop manuel, alpha 32 bits). Rendu en perspective axonométrique 3/4 face (3 tons de brun), unique parmi les capteurs en silhouette frontale. **Surveillance quantization** : la palette devrait réduire la boîte à 2 tons (base + ombre) pour atténuer l'effet 3D. |
| `eddy_covariance_tower.png` | Nanobanana | **généré, détourage à refaire** | Tour eddy covariance, treillis ouvert. Source détourée présente (2572×1632, alpha 32 bits) mais **contamination magenta résiduelle dans le treillis** (~20% des pixels opaques en halos rose-magenta `#9D5083`-ish, soit ~26 700 px sur 133 K). Le détourage manuel n'a pas utilisé une tolérance baguette assez élevée (cible 70-80) pour absorber l'anti-aliasing du treillis. Action DA : retourer le détourage avant intégration. La tour est exclue de l'extraction palette `v1.0` jusqu'à correction (sinon le centroïde rose-magenta pollue la palette). |

---

## 2. Icônes UI

Source : **Lucide Icons** (https://lucide.dev), import direct, pas de
génération IA. Licence ISC, libre d'usage.

### Contrôles temps

- `Play`
- `Pause`
- `FastForward`
- `SkipForward`

### Climat

- `Droplet`
- `Thermometer`
- `Wind`
- `Sun`

### Biodiversité

- `Bird`
- `Bug`
- `Sprout`
- `TreePine`

### Économie

- `TrendingUp`
- `Coins`
- `Calculator`

### Événements

- `AlertTriangle`
- `Bell`
- `Info`

**Statut global** : à intégrer (téléchargement direct depuis
lucide.dev, format SVG converti en sprite Unity).

---

## 3. Plan minimap

Source : **fait main** (Inkscape ou Figma), dessin vectoriel exporté
en SVG ou PNG haute résolution.

| Élément | Description |
|---|---|
| Contour de parcelle | Ligne fermée représentant les limites du site. |
| Haies | Lignes fines, variations d'épaisseur selon densité. |
| Mare | Cercle / forme libre. |
| Bosquet | Zone teintée. |
| Arbre têtard | Symbole ponctuel (cercle plein). |
| Chemins | Lignes pointillées. |
| Capteurs | Symboles géométriques distincts par type (cercle, carré, triangle). |

**Statut** : à dessiner.

**Responsable** : utilisateur (cf `CLAUDE.md` §2 — division du travail).

---

## 4. Particules Unity

Configurées dans Unity, pas d'asset externe.

| Effet | Notes |
|---|---|
| Feuilles dérivantes au vent | Particle System, sprite simple, modulé par variable météo (force du vent). |
| Poussières dans la lumière | Particle System ambient, modulation densité par variable d'humidité. |

**Statut** : à configurer dans Unity (étape 9).

---

## 5. Polices

À télécharger et placer dans `Assets/_Project/Fonts/`.

| Police | Source | Licence | Usage |
|---|---|---|---|
| **EB Garamond** | Google Fonts | OFL (Open Font License) | Titres, labels, tooltips italique. |
| **JetBrains Mono** | jetbrains.com/lp/mono | Apache 2.0 | Valeurs chiffrées. |
| **IBM Plex Mono** (alternative) | Google Fonts | OFL | Alternative à JetBrains Mono. |

**Étape supplémentaire** : générer les assets TMP SDF via
`Window > TextMeshPro > Font Asset Creator`. Atlas 1024×1024, inclure
caractères latins étendus (accents français, €).

**Statut** : à télécharger et convertir.

---

## 6. Pipeline de génération IA

### Style guide visuel

- **Prompt template** : à définir et conserver dans
  `Sprites/Source/PROMPT_TEMPLATE.md` (créé à l'étape 9 si non déjà
  établi).
- **Image de référence stylistique** : à générer en premier, sert de
  référence ip-adapter pour tous les sprites suivants.
  - Nom : `style_reference.png`
  - Stockage : `Sprites/Source/style_reference.png`
  - Critères : esprit Charles Harper + chaleur A Short Hike +
    palette colombages percherons (cf `DECISIONS.md` #3).

### Étapes pour chaque sprite

1. Génération sur Nanobanana avec ip-adapter pointant sur l'image-ancre
   stylistique (`Sprites/Source/01_anchor_full_scene.png`).
2. Sortie brute archivée dans `Sprites/Source/<name>_v<n>.png`
   (hors `Assets/`, racine du repo, pour ne pas alourdir l'import
   Unity ni le hash de cache CI).
3. Détourage manuel par l'utilisateur (Photoshop / GIMP) ; sortie
   archivée à côté avec suffixe `_detoured.png`.
4. Post-traitement automatique via `python tools/postprocess.py
   <source>_detoured.png <destination>.png` :
   - **Chroma cleanup** (étape 1, ajoutée 2026-05-12) : force alpha à 0
     sur les pixels strictement magenta pur (R>240, G<30, B>240) ou vert
     pur (R<30, G>240, B<30). Filet de sécurité pour les sprites où le
     détourage manuel aurait laissé des pixels chroma-key isolés
     (typiquement les sujets à treillis ouvert). Ne sauve PAS la color
     decontamination (pixels en anti-aliasing semi-magenta tintés
     rose-brun) — pour ces cas, c'est le détourage utilisateur qui doit
     être correct dès le départ (wand tolerance 70-80).
   - Alpha cleanup (snap < 30 → 0, > 230 → 255, conserve les bords
     anti-aliasés).
   - Palette quantization sur la palette Perche (`tools/palette_perche.json`).
   - Crop au bounding box alpha + resize au longest-side cible
     (`--max-size`, défaut 512 px).
5. Export dans le sous-dossier thématique
   (`Background/`, `Midground/`, `Foreground/`, `Fauna/`, `Sensors/`).
6. Validation visuelle DA avant intégration.

### Palette Perche

Définie dans `tools/palette_perche.json`. Statut : **`v1.0` (candidat,
en attente de validation DA finale)** au 2026-05-12.

**Méthode** : k-means à 32 couleurs (RGB), sous-échantillonnage
équilibré à 25 000 pixels opaques par sprite source (alpha ≥ 200) pour
que chaque sprite contribue à parts égales — un gros sprite haie
(2-3 MP) n'écrase pas les signatures chromatiques rares mais
fonctionnellement critiques des petits sprites (anneau métallique du
piézomètre, charcoal de la lentille piège photo, etc.). Reproductible
via :

```
python tools/extract_palette.py --sources-glob "Sprites/Source/*_detoured.png" \
    --exclude "Sprites/Source/01_anchor_full_scene.png" \
    --exclude "Sprites/Source/eddy_covariance_tower_v1_detoured.png" \
    --colors 32 --version-tag v1.0
```

**Exclusions** :

1. `01_anchor_full_scene.png` — contient aussi les couleurs UI du
   mockup (anthracite, ivoire) qui ne doivent pas polluer la palette
   scène.
2. `eddy_covariance_tower_v1_detoured.png` — contamination magenta
   résiduelle dans le treillis (~20 % de pixels opaques) due à un
   détourage à tolérance baguette insuffisante. À ré-intégrer dans la
   palette après re-détourage côté DA.

**Concerns connus à arbitrer en validation DA** :

- **Joncs autour de la mare en vert saturé** (`pond_v1_detoured.png`) :
  ~58 K pixels en `#347C2F`-ish. La spec DA demande à ce que la
  quantization les ramène à l'olive sourd des haies, mais ils sont
  numériquement assez denses pour former leur propre centroïde
  (~954 px dans la palette finale, soit la couleur la moins
  populée mais quand même présente). Trois options DA :
  (a) accepter — les joncs restent dans leur teinte saturée ;
  (b) supprimer manuellement la couleur du JSON et re-quantizer — les
  joncs se mappent sur l'olive voisin ;
  (c) ajouter un filtre de saturation au sampling pour exclure ces
  pixels — palette régénérée sans ce centroïde.
- **Absence du cream-ivoire chaud** (`#E8DDC4`) : la palette `v1.0`
  contient `#DFE1DF` (cool white) et `#C9A27D` (warm beige) mais pas
  exactement le ton ambré débattu en DA pour le ventre d'hirondelle.
  Impact limité : `swallow.png` est déjà intégré avec la
  `v0.1-provisional` et conserve son ton ; aucun autre sprite du corpus
  n'a clairement besoin de cette nuance.

**Sprite déjà passé sur la palette `v0.1-provisional`** : `swallow.png`
(intégré). Peut être re-quantizé sur la `v1.0` après validation, mais
non bloquant.

---

## 7. Stratégie en cas d'échec génération IA

Hiérarchie des solutions (du plus acceptable au plus douloureux) :

1. **Réduire la complexité du prompt** (simplifier la description,
   réduire le nombre de détails attendus).
2. **Réduire le nombre de variantes** (passer de 2 variantes de haie
   basse à 1).
3. **Fusionner avec un autre sprite** (réutiliser un sprite voisin avec
   variation par shader).
4. **En tout dernier recours** : utiliser des assets libres
   (Kenney.nl, OpenGameArt) puis appliquer le post-traitement Python
   pour les rendre cohérents avec la palette Perche.

**Note** : ne jamais mélanger sprites IA et sprites externes sans
post-traitement uniformisant — la cohérence visuelle est un critère
non-négociable de l'étape 9.
