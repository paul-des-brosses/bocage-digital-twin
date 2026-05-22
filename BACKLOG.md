# BACKLOG.md — Items reportés hors scope v1

Items écartés explicitement de la première publication (fin de l'Étape 10),
avec, pour chacun, une note d'implémentation suffisante pour qu'un futur
contributeur (ou Claude Code, ou toi) puisse reprendre le travail sans
re-faire la phase d'analyse.

L'ordre dans le document n'est pas un ordre de priorité — chaque item
porte sa propre estimation et ses dépendances.

---

## 1. Faune statique en pool, densité pilotée par la biodiversité

**Pourquoi reporté** : choix utilisateur à la fin de l'Étape 8 — focus
sur la livraison fonctionnelle d'un Digital Twin honnête plutôt que sur
le polish visuel pour la v1.

**Sprites prêts** : `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/`
contient 4 espèces (`harrier.png`, `heron.png`, `owl.png`,
`swallow.png`).

**Architecture cible** (héritée du pattern `SceneCompositionDefinition`
+ `SceneAssembler`) :

- `Assets/_Project/05_Presentation/Scene/Fauna/FaunaSpeciesDefinition.cs`
  → ScriptableObject par espèce (`speciesId`, `sprite`,
  `sortingLayerName`, `sortingOrderInLayer`, `maxPoolSize`, `baseScale`,
  `scaleJitter`, `placementZone` (Rect monde), `biodiversityResponse`
  (AnimationCurve : x = biodiv [0,1] → y = ratio pool actif [0,1])).
- `Assets/_Project/05_Presentation/Scene/Fauna/FaunaPlacementDefinition.cs`
  → SO racine listant les espèces.
- `Assets/_Project/05_Presentation/Scene/Fauna/FaunaPool.cs`
  → MonoBehaviour ; Awake : pré-instancie `maxPoolSize` sprites par
  espèce sous `spawnRoot`, positions déterministes via
  `SeededRandom.DeriveSubStream("fauna_placement")` à partir d'un seed
  exposé en inspector. Start : s'abonne à
  `RC_BiodiversityComposite.OnChanged`, applique la courbe. **Pas
  d'Instantiate/Destroy runtime** (CLAUDE.md §6) — uniquement
  `SetActive`.

**Assets attendus** :
- 4 `FaunaSpecies_*.asset` dans `Assets/_Project/Data/Fauna/`.
- 1 `FaunaPlacement_Default.asset` qui les référence.
- Un GameObject `_Scene_Visual/Fauna` avec `FaunaPool` câblé.

**Estimation** : 0.5 jour.

---

## 2. Animation faune (idle motion)

**Pourquoi reporté** : dépend de l'item 1.

**Architecture cible** : un composant `FaunaIdleMotion` collé à chaque
sprite pool member. Comportement par espèce :
- **swallow / harrier** : oscillation horizontale sinusoïdale lente
  (vol stationnaire stylisé) + très légère amplitude verticale.
- **owl** : pas d'animation (perché).
- **heron** : sway vertical très lent (respiration).

Paramètres exposés par espèce via `FaunaSpeciesDefinition.idleMotion`
(amplitude, période, déphasage déterministe par index dans le pool
pour éviter la synchronisation).

**Cible perf** : zéro allocation, lecture `Time.time` une fois par
Update, math.Sin only. Toujours pas d'Update sur les pool members
inactifs.

**Estimation** : 0.5 jour après l'item 1.

---

## 3. Modulation healthT sur la faune

**Pourquoi reporté** : dépend de l'item 1.

**Idée** : un binding `FaunaShaderBinding` analogue à
`HedgerowShaderBinding`. Lit `RC_BiodiversityComposite` (déjà câblé)
et pousse une désaturation / pâleur sur la couleur des sprites quand
l'index est bas. Pas de shader dédié — un material partagé `M_Fauna`
avec un shader `S_Fauna` (HLSL, mêmes propriétés `_Color` + `_HealthT`
que le pattern haie).

**Estimation** : 0.5 jour après l'item 1.

---

## 4. Particules Unity (feuilles, poussières)

**Pourquoi reporté** : choix utilisateur — polish visuel pour v2.

**Cible** :
- Feuilles dérivantes en automne (densité pilotée par
  `RC_HedgerowHealth` — feuilles d'autant plus nombreuses que la santé
  baisse, mimant le dépérissement).
- Poussière dans la lumière au-dessus des chemins (densité pilotée
  par `1 - RC_SoilMoisture` — plus le sol est sec, plus la poussière
  se lève).

**Contrainte** : pas de threading (WebGL), pas de simulation physique
lourde. Particle System Unity standard, pre-warm activé.

**Estimation** : 0.5 jour.

---

## 5. SG_Hedgerow — node `_HealthT` natif

**Pourquoi reporté** : sub-étape 9β a câblé le binding pour pousser
`_HealthT` via `MaterialPropertyBlock`, mais le Shader Graph
`SG_hedgerow.shadergraph` ne lit pas encore cette propriété. Unity
ignore silencieusement les SetFloat sur propriétés inconnues, donc le
canal est dormant côté visuel pour l'instant.

**Action manuelle Unity** (5–10 min) :
1. Ouvrir `Assets/_Project/05_Presentation/Scene/Shaders/SG_hedgerow.shadergraph`.
2. Ajouter une propriété blackboard `_HealthT` (Float, Default 1.0,
   Range [0,1]).
3. Avant le node Color final, insérer un Lerp :
   - A = couleur "saine" (verte) déjà calculée
   - B = couleur "stressée" (brune / désaturée, à choisir)
   - T = `1 - _HealthT`
4. Sauvegarder. Le binding pousse déjà `_HealthT` à chaque tick : effet
   visible immédiatement.

**Estimation** : 10 min.

---

## 6. Effets visuels avancés mare et prairie

**Pourquoi reporté** : la v1 fait juste un lerp de couleur. Les
`.shader` actuels sont étendables sans bouleverser l'architecture
(les bindings sont indépendants du contenu du shader).

**Idées concrètes** :
- **S_Pond** : rides sinusoïdales basse fréquence sur l'alpha,
  modulées par `_WaterLevel`. Reflet de ciel (sampling de
  `RC_Weather`).
- **S_Meadow** : variation florale (clusters de petites taches
  colorées) modulée par `RC_SoilMoisture` — sol humide = densité florale
  plus élevée. Possiblement migration vers Shader Graph si l'effet
  passe le seuil de complexité où la preview live aide.

**Estimation** : 0.5–1 jour par shader.

---

## 7. Animations UI (transitions, micro-interactions)

**Pourquoi reporté** : la v1 est fonctionnelle, pas léchée. Les
panneaux apparaissent durs, les changements de KPI ne sont pas
animés, les hover sont binaires.

**Cibles** :
- Fade-in 200 ms des panneaux à l'ouverture.
- Tween des valeurs numériques (`Mathf.Lerp` côté binding pour les
  labels).
- Pulse léger sur les cartouches Hero KPI quand la valeur a bougé
  significativement (>10 % depuis dernière publication).

**Estimation** : 0.5 jour.

---

## 8. SessionReporter accessible depuis l'UI

**Pourquoi reporté** : item explicite de l'Étape 10 mais pas critique
pour une première publication démontrable.

**Cible** : bouton "Exporter la session" dans le dashboard qui
sérialise (en JSON dans la console / un fichier WebGL téléchargeable)
le déroulé : seed, scénario, journal de décisions, courbes des Hero
KPIs.

**Estimation** : 0.5 jour.

---

## Liens cross-document

- Items 1–3 (faune) : étaient partiellement décrits dans
  `ROADMAP.md` § Étape 9 livrable #2 et #3.
- Items 4 : `ROADMAP.md` § Étape 9 livrable #6.
- Item 5 : sortie naturelle de la sub-étape 9β (cf `DECISIONS.md` #42).
- Item 8 : `ROADMAP.md` § Étape 10 livrable « SessionReporter
  accessible depuis l'UI ».

Tout item ajouté au backlog doit pointer vers la décision ou le
livrable d'origine pour ne pas perdre la traçabilité.
