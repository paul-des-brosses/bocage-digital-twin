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

## 8. Enrichir les leviers décisionnels de l'agriculteur

**Pourquoi reporté** : la v1 livre 3 types de recommandations (replanter
haies, irrigation, baisser intensité d'intrants) déclenchées par 3
événements (chalara, drought, anomalie acoustique faune). C'est
suffisant pour démontrer la chaîne capteur → événement → reco →
arbitrage → impact, mais l'« espace agriculteur » du dashboard est
visuellement maigre par rapport au « cadre extérieur » à 6 sliders.

**État actuel** :
- `RecommendationEngine.TryProduceFor` (Couche 3) — switch event type
  → reco type. 3 cas.
- `AutoActionPipeline` applique 3 effets mécaniques.
- `decision-panel` UXML : 2 sliders pratiques quotidiennes (arrachage
  haies, intensité intrants) + slider horizon + bouton historique.

**Pistes d'extension** :
- **Nouveaux types d'événements** : excès de pluviométrie (waterlogging
  des cultures), pression ravageurs (lecture caméra), épuisement
  fertilité sol (anomalie sur CropYield trend).
- **Nouvelles recommandations** : couverts végétaux d'interculture,
  agroforesterie inter-rangs, drainage léger, fauche tardive,
  reconnexion mare/fossé.
- **Nouveaux leviers continus** dans le panneau de droite : taux de
  couverture en cultures intermédiaires, calendrier de fauche, ratio
  prairies permanentes/temporaires.
- **Effets mécaniques** : chaque levier touche une variable d'état
  existante d'`EcosystemModel` (ou en ajoute une nouvelle si justifié
  par CLAUDE.md §9).

**Garde-fous** :
- Toute nouvelle reco doit être déclenchée par un événement traçable
  à un capteur (cf CLAUDE.md §9). Pas de reco « parce que c'est
  octobre ».
- Chaque levier doit avoir une calibration sourcée (Solagro, INRAE,
  PNR Perche). Pas de paramètre arbitraire.

**Estimation** : 1 jour par type de reco supplémentaire (event
detector + reco + outcome projector + auto-action + UI). Couvrir 2-3
types est raisonnable post-v1.

---

## 9. Capital d'investissement dans le modèle économique

**Pourquoi reporté** : `IntegratedProfitabilityIndicator` agrège
aujourd'hui revenu cultures − coûts intrants − coûts entretien
+ paiement services écosystémiques. Aucune notion d'investissement
amortissable ni de capital initial (matériel, plantation initiale du
linéaire de haies, drainage, matériel de fauche tardive…). La métrique
est donc une rentabilité « opérationnelle annuelle » plutôt qu'une
rentabilité intégrée au sens financier.

**Conséquence** : les recommandations qui coûtent gros à
l'investissement (replanter 30 m/ha de haies = ~30×coûts d'achat
plants + main d'œuvre) ne pèsent pas sur le KPI Rentabilité. L'arbitrage
agriculteur est faussé vers l'acceptation systématique.

**Pistes** :
- Variable d'état `CumulativeCapitalEur` dans `EcosystemModel`,
  incrémentée à chaque AutoAction par un coût de mise en œuvre sourcé
  (PNR Perche : ~12 €/m de haie plantée, etc.).
- Amortissement linéaire sur 20 ans dans `IntegratedProfitabilityIndicator`
  (ou autre durée selon la nature de l'investissement).
- Distinguer dans l'UI rentabilité opérationnelle vs intégrée
  capital-amorti.

**Garde-fous** :
- Calibration sourcée (PNR Perche, AFAC-Agroforesteries, CIVAM).
- Cohérence avec les AutoActions existantes : chacune doit déclarer
  son coût de mise en œuvre en €/unité.

**Estimation** : 0.5–1 jour.

---

## 10. SessionReporter accessible depuis l'UI

**Pourquoi reporté** : item explicite de l'Étape 10 mais pas critique
pour une première publication démontrable.

**Cible** : bouton "Exporter la session" dans le dashboard qui
sérialise (en JSON dans la console / un fichier WebGL téléchargeable)
le déroulé : seed, scénario, journal de décisions, courbes des Hero
KPIs.

**Estimation** : 0.5 jour.

---

## 11. Popup explicative du KPI Delta tech avec mini-chart real vs shadow

**Pourquoi reporté** : le KPI Delta tech est aujourd'hui un chiffre
nu (avec caption « Réel vs run fantôme »). Un visiteur qui veut
visualiser la divergence des deux trajectoires n'a pas d'accès direct
à l'historique courbe.

**Cible** : ajouter un petit picto `(i)` à côté du libellé « Delta
tech » dans la cartouche. Au clic, ouvrir une popup centrée qui
contient :
- Un texte court expliquant ce qu'est la run fantôme et ce qui est
  mesuré.
- Un mini-chart 400×140 px tracé en runtime (UI Toolkit
  `MeshGenerationContext` ou IMGUI custom) : les 60 ou 90 derniers
  jours, deux lignes superposées (real solide, shadow pointillé)
  sur la rentabilité intégrée €/ha/an.
- Un bouton Fermer.

**Pré-requis** : `SimulationRunner` doit garder un historique
glissant des deux trajectoires (deux ring-buffers de 90 floats par
exemple), ce qui n'existe pas aujourd'hui — les KPIs sont publiés
mais pas archivés.

**Estimation** : 4 h (ring-buffer + popup UXML + draw du chart).

---

## 12. Phénologie cultures + saisonnalité du rendement

**Pourquoi reporté** : le manque scientifique le plus visible aux
yeux d'un agroécologue qui ouvrirait la démo. Aujourd'hui
`CropYield` dérive en continu via `CropYieldDynamicsRule` sans pic
ni creux saisonnier — pas de semis, pas de récolte, pas de fenêtre
critique de stress hydrique.

**Cible** :
- Ajouter `GrowingDegreeDays` (somme T° base 6 °C depuis semis) en
  variable d'état dérivée de `CurrentWeather.TemperatureCelsius`.
- Fenêtre semis (jour 280 = octobre pour blé d'hiver, ou paramétré)
  et fenêtre récolte (cumul GDD seuil).
- `CropYieldDynamicsRule` doit reconnaître les phases : croissance
  active vs dormance vs récolte (drop à 0 puis re-build).
- Ouvre un nouveau type d'événement « stress hydrique en phase
  reproductive » avec recommandation associée.

**Calibration** : INRAE échelle BBCH blé, ARVALIS chiffres Eure-et-Loir.

**Estimation** : 1.5–2 jours.

---

## 13. Variable d'état Carbone sol

**Pourquoi reporté** : ouvrirait un sixième Hero KPI possible
(« Stockage carbone ») et donnerait une dimension climat-mitigation
à la rentabilité (Label Bas-Carbone, ~30 €/tCO2 stockée).

**Cible** :
- `SoilOrganicCarbon` (t C/ha) dans `EcosystemModel`. Baseline ~50 t
  C/ha pour un sol bocager Perche.
- Règle de dynamique : +stock via litière haies (linéaire en
  `HedgerowDensity`), −stock via labour intense (linéaire en
  `InputIntensityFactor` au-dessus de 1.0), modulation T° pour la
  minéralisation accélérée.
- Indicateur `CarbonStockIndicator` agrégeant et normalisant.
- Container `RC_SoilCarbon` observable.

**Calibration** : INRA 4 pour 1000, ADEME, AFAC-Agroforesteries
(0.4 tC/ha/an stockable sous haies denses).

**Estimation** : 1 jour.

---

## 14. Couplage sécheresse → chalara

**Pourquoi reporté** : aujourd'hui les deux événements (chalara,
drought) sont détectés indépendamment alors que la littérature
INRAE montre que les frênes stressés hydriquement sont plus
susceptibles à *Hymenoscyphus fraxineus*. Le couplage rendrait la
chaîne « météo → nappe → stress arbre → chalara » plus crédible.

**Cible** : moduler `EventDetector.HedgeAlertThresholdMetersPerHectare`
(ou ajouter une condition multiplicative) : si une `DroughtProlongedEvent`
est active dans les 60 derniers jours, le seuil chalara remonte
(par ex. de 75 à 85 m/ha) — donc la détection se déclenche plus
facilement. Documenter dans DECISIONS.

**Calibration** : INRAE chalara monitoring (cite la corrélation
stress hydrique × susceptibilité fraxinus).

**Estimation** : 0.3 jour.

---

## 15. Refonte simulation Biodiversité

**Pourquoi reporté** : `BiodiversityCompositeIndicator` agrège
aujourd'hui 50 % fauna abondance + 30 % densité haies + 20 % nappe
(inversée) — pondérations auto-justifiées sans citation précise.
`FaunaPopulation` est un indice scalaire sans diversité, sans
structure trophique.

**Cible** : refonte vers une simulation biodiversité plus réaliste.
À cadrer en détail avec un agronome ou écologue ; pistes initiales :
- Distinguer faune utile (pollinisateurs, prédateurs ravageurs) vs
  faune patrimoniale (oiseaux farmland, amphibiens).
- Boucle prédation : haute faune utile → moins de ravageurs → moins
  de besoin intrants (rétroaction).
- Pondérations sourcées (INRAE workshops, Vigie-Nature panels).
- Possiblement nouveaux indicateurs sub-Hero (Pollinisateurs,
  Auxiliaires, Espèces patrimoniales) câblés depuis des variables
  d'état dédiées.

**Pré-requis** : décider de la granularité (1 vs N indices) après
lecture de `SIMULATION_OVERVIEW.md` et discussion avec un référent.

**Estimation** : à cadrer (1-3 jours selon ambition).

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
- Item 8 (leviers agriculteur) : audit sub-étape 10a, friction sur
  la maigreur visuelle de l'« espace agriculteur » comparé au cadre
  extérieur.
- Item 9 (capital d'investissement) : audit sub-étape 10a, friction
  sur la métrique Rentabilité intégrée qui ne tient pas compte du
  capital investi par les AutoActions.
- Item 10 (SessionReporter) : `ROADMAP.md` § Étape 10 livrable
  « SessionReporter accessible depuis l'UI ».
- Items 11-15 : issus de l'audit complet du DT (sub-étape 10a).
  - #11 (popup chart real-vs-shadow) : audit zone tendue
    « divergence invisible autrement qu'en pourcentage ».
  - #12 (phénologie) : audit ouverture saisonnalité.
  - #13 (carbone sol) : audit nouvelles variables d'état.
  - #14 (couplage drought → chalara) : audit couplages absents.
  - #15 (refonte biodiversité) : audit pondérations
    `BiodiversityCompositeIndicator` non sourcées.

Tout item ajouté au backlog doit pointer vers la décision ou le
livrable d'origine pour ne pas perdre la traçabilité.
