# ASSETS_LIST.md — Liste des assets visuels

Inventaire exhaustif des assets nécessaires au projet. Statut à mettre
à jour au fil de la production : `à générer` → `généré` → `post-traité`
→ `intégré`.

---

## 1. Sprites scène

### Background

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hills_perche.png` | Nanobanana | à générer | Paysage de collines vallonnées du Perche, palette ocre-vert sourd, format large pour fond de scène. |

### Midground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hedge_low_01.png` | Nanobanana | à générer | Haie basse, variante 1. |
| `hedge_low_02.png` | Nanobanana | à générer | Haie basse, variante 2 (variation visuelle). |
| `hedge_high_pollard_01.png` | Nanobanana | à générer | Haie haute avec arbre têtard, variante 1. |
| `hedge_high_pollard_02.png` | Nanobanana | à générer | Haie haute avec arbre têtard, variante 2. |
| `hedge_thin_sparse.png` | Nanobanana | à générer | Haie clairsemée, pour variation visuelle (état dégradé). |

### Foreground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `pollard_ash_main.png` | Nanobanana | à générer | Arbre têtard de frêne, élément iconique, premier plan. |
| `pond.png` | Nanobanana | à générer | Mare avec bords, sprite avec zone d'eau modulable par shader. |
| `grass_border.png` | Nanobanana | à générer | Bordure de prairie premier plan. |

### Fauna

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `bird_swallow_flight.png` | Nanobanana | intégré | Hirondelle en vol, sprite simple animable. Validation DA 2026-04-26 (option 1 — palette `v0.1-provisional` retenue sans rééquilibrage). Sprite final : `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/swallow.png` (256×121). |
| `bird_owl_flight.png` | Nanobanana | à générer | Chouette chevêche en vol. |
| `bird_harrier_flight.png` | Nanobanana | à générer | Busard en vol. |
| `heron_static.png` | Nanobanana | à générer | Héron au bord de la mare, statique. |
| `amphibian_small.png` | Nanobanana | à générer | Sprite amphibien (grenouille / triton), optionnel — coupable en cas de dépassement. |

### Sensors (visibles dans la scène)

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `weather_station.png` | Nanobanana | à générer | Station météo, mât avec capteurs. |
| `piezometer.png` | Nanobanana | à générer | Piézomètre, tube de mesure de nappe. |
| `acoustic_sensor.png` | Nanobanana | à générer | Capteur acoustique, micro directionnel. |
| `photo_trap.png` | Nanobanana | à générer | Piège photo, boîtier sur tronc. |
| `eddy_covariance_tower.png` | Nanobanana | à générer | Tour eddy covariance, élément technique haut. |

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
   - Alpha cleanup (snap < 30 → 0, > 230 → 255, conserve les bords
     anti-aliasés).
   - Palette quantization sur la palette Perche (`tools/palette_perche.json`).
   - Crop au bounding box alpha + resize au longest-side cible
     (`--max-size`, défaut 512 px).
5. Export dans le sous-dossier thématique
   (`Background/`, `Midground/`, `Foreground/`, `Fauna/`, `Sensors/`).
6. Validation visuelle DA avant intégration.

### Palette Perche

Définie dans `tools/palette_perche.json`. Statut : **`v0.1-provisional`**
au 2026-04-26.

Composition : 24 couleurs extraites par k-means sur l'image-ancre
(scène crépusculaire dominée par dusk-blues, olive et bronze) + 6
couleurs d'accent ajoutées manuellement pour couvrir les gaps connus
(crème ventre fauna, olive vif haies saines, ocre chaud accents
soleil, bleu-gris nuage, etc.). Total 30 couleurs.

Protocole de validation : sera figée comme `v1.0` une fois validée
sur 3 sprites stylistiquement différents — (1) hirondelle ✅, (2)
`hedge_low_01` ⏳, (3) `pond.png` ⏳. Un éventuel ton bleu-eau
supplémentaire sera évalué au stade `pond.png`.

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
