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

## 5. ~~SG_Hedgerow — node `_HealthT` natif~~ — livré

**Statut** : ✅ livré en sub-étape 10b (audit MVP). Le Shader Graph
`SG_hedgerow.shadergraph` lit désormais la propriété `_HealthT` via un
second Lerp inséré entre le mix densité et le multiply texture :
- A = sortie du Lerp_density (couleur saine, dérivée de `_Density`)
- B = couleur stressée (R 0.55 / G 0.50 / B 0.35, brun-ocre)
- T = `1 - _HealthT` (via node `One Minus`)

Le canal data était déjà câblé par `HedgerowShaderBinding` depuis 9β ;
cet item ne nécessitait plus que le geste manuel dans l'éditeur Shader
Graph. La rubrique est conservée en historique pour traçabilité.

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

## 12. Saisonnalité météo + phénologie cultures + saisonnalité du rendement

**Pourquoi reporté** : le manque scientifique le plus visible aux
yeux d'un agroécologue qui ouvrirait la démo. Aujourd'hui
`CropYield` dérive en continu via `CropYieldDynamicsRule` sans pic
ni creux saisonnier — pas de semis, pas de récolte, pas de fenêtre
critique de stress hydrique. En amont, `WeatherUpdateRule` tire
chaque jour autour de moyennes annuelles fixes (12 °C, 2 mm/jour)
sans aucun cycle saisonnier — le jour 1 et le jour 180 ont la même
distribution météo, ce qui rend toute phénologie culturale inepte.

**Cible** :
- **Pré-requis — saisonnalité dans `WeatherUpdateRule`** : remplacer
  les constantes `BaseTemperatureC` et `BasePrecipitationMm` par des
  courbes annuelles sinusoïdales en fonction de `model.CurrentDay % 365`.
  Forme cible :
  ```
  T_mean(d) = 12 + 7 × sin(2π × d / 365 − π/2)   // min ≈ 5 °C jan, max ≈ 19 °C juil
  P_mean(d) = 2 × (1 − 0.3 × cos(2π × d / 365))  // été légèrement plus sec
  ```
  Les anomalies scénario continuent à s'appliquer comme décalages
  additifs sur ces moyennes. Le bruit gaussien reste inchangé.
  Calibration : normales Météo-France Eure-et-Loir (T° moy. jan ≈ 4 °C,
  juil ≈ 19 °C ; précipitations légèrement déficitaires en été).
- Ajouter `GrowingDegreeDays` (somme T° base 6 °C depuis semis) en
  variable d'état dérivée de `CurrentWeather.TemperatureCelsius`.
- Fenêtre semis (jour 280 = octobre pour blé d'hiver, ou paramétré)
  et fenêtre récolte (cumul GDD seuil).
- `CropYieldDynamicsRule` doit reconnaître les phases : croissance
  active vs dormance vs récolte (drop à 0 puis re-build).
- Ouvre un nouveau type d'événement « stress hydrique en phase
  reproductive » avec recommandation associée.

**Calibration** : INRAE échelle BBCH blé, ARVALIS chiffres Eure-et-Loir,
normales Météo-France Eure-et-Loir pour les amplitudes T° et précip.

**Estimation** : 2–2.5 jours (0.5 jour `WeatherUpdateRule` + 1.5–2 jours phénologie).

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

## 16. Détection du chalara avec un capteur adapté

**Pourquoi reporté** : la détection de `HedgeChalaraEvent` a été
retirée de `EventDetector` avant la publication v1 car elle était
attribuée au piège photo (capteur faune infrarouge), ce qui est
sémantiquement faux — un piège photo ne peut pas détecter un champignon
parasite sur des frênes. La classe `HedgeChalaraEvent.cs` et la
recommandation `PlantHedgesRecommendation.cs` sont conservées en
attente d'un capteur réaliste.

**Ce que chalara nécessite réellement** : le dépérissement du frêne
(*Hymenoscyphus fraxineus*) se détecte visuellement via défoliation
de la couronne, nécrose de l'écorce et dépérissement des pousses.
Les méthodes de terrain réalistes sont :

- **Drone multispectral / hyperspectral** — indice NDVI ou RED EDGE
  sur le linéaire bocager. Détecte la perte de vigueur foliaire avant
  qu'elle soit visible à l'œil nu.
- **Enquête de terrain périodique** (simulée) — un capteur
  `FieldSurveyProbe` déclenché tous les N jours, avec taux de
  détection < 1.0 (probabilité de rater un foyer de taille modeste).
- **Piège à spores** (plus exotique, non retenu pour la v1).

**Architecture cible** :
- Nouveau capteur `sensor_ndvi_drone_01` dans
  `SensorPlacement_Default.asset`, `observedModelVariable :
  HedgerowDensity` (proxy de vigueur).
- Classe `HedgerowVigorSensorReading` en Couche 2 avec bruit
  asymétrique (sous-détection plus probable que sur-détection).
- `EventDetector` réintègre `HedgeChalaraEvent` mais basé sur la
  lecture bruitée du nouveau capteur, pas sur la valeur vraie du
  modèle.
- Couplage optionnel avec item #14 (sécheresse → susceptibilité
  accrue au chalara).

**Calibration** : INRAE chalara monitoring (protocoles de surveillance
phytosanitaire frênaies du Perche), ONF surveillance sanitaire forêts.

**Estimation** : 1 jour (capteur + lecture bruitée + réintégration
EventDetector + tests).

---

## 17. Réalisme avancé des capteurs faune

**Pourquoi reporté** : pour le MVP, le piège photo et le capteur
acoustique partagent un profil de bruit identique et statique
(σ = 0.20 / √fauna). C'est suffisant pour introduire de l'incertitude
dans la détection et crédibiliser le Delta tech, mais insuffisant pour
simuler les vraies limites de chaque technologie de terrain.

**Cibles** :
- **Signal acoustique dégradé par la météo** : vent fort ou pluie
  intense masquent les chants d'oiseaux et le stridulation des
  orthoptères. Lire `CurrentWeather.PrecipitationMm` et une vitesse
  de vent simulée (à ajouter dans `CurrentWeather`) pour augmenter
  dynamiquement le sigma du capteur acoustique au-dessus de seuils
  calibrés. Pré-requis : backlog #12 (saisonnalité météo) pour que
  les épisodes pluvieux soient structurés.
- **Piège photo moins efficace en bocage très dense** : au-delà de
  ~130 m/ha la végétation réduit les couloirs de détection (paradoxe :
  plus de haies = meilleur habitat faune, mais moins de visibilité
  pour le capteur). Moduler le sigma du cameraTrap en fonction de
  `HedgerowDensity` au-dessus du seuil.
- **Biais saisonniers** : les oiseaux sont plus vocaux au printemps
  (reproduction), les amphibiens plus détectables en période humide.
  Nécessite le cycle saisonnier de backlog #12 comme pré-requis.
- **Score de confiance affiché dans l'UI** : exposer l'intervalle de
  confiance à 95 % de la lecture combinée dans le panneau "Capteurs
  déployés" du dashboard, pour rendre l'incertitude visible à
  l'utilisateur.

**Pré-requis** : backlog #12 (saisonnalité météo) pour les deux
derniers points.

**Estimation** : 1 jour.

---

## 18. Recalibrer `MaintenanceCost` selon le référentiel AFAC 2024

**Pourquoi reporté** : la valeur actuelle de 1 €/m/an est défendable
comme coût out-of-pocket pour un agriculteur qui auto-réalise ses
travaux de haies, mais elle est 3 à 5× inférieure aux références
sectorielles :

- **Référentiel Réseau Haies 2024** : coût moyen de gestion durable
  3,69 €/ml (vs 3,32 €/ml en 2019).
- **Avec indice prestation 2024 (41 %)** : coût total moyen de gestion
  (manuelle + mécanisée + prestation) ≈ 5,19 €/ml.
- **Amendement Sénat novembre 2025** : AFAC-Agroforesteries cite 4,5 €/ml.

L'écart n'est pas un bug mais un choix de scope : le modèle simplifie
en ne distinguant pas travail auto-absorbé et coût de prestation
externalisé. Le résultat est que `MaintenanceCost` sous-estime le coût
réel d'entretien d'une ferme qui fait appel à des prestataires, ce qui
biaise la rentabilité à la hausse.

**Cible** : rendre le taux paramétrable dans `ScenarioContext` (slider
"mode d'entretien" de type auto-réalisation 1 €/ml à prestation 5 €/ml),
ou distinguer deux régimes dans `MaintenanceCostDynamicsRule`. Lier à
l'item #9 (capital d'investissement) puisque les deux touchent au modèle
économique.

**Calibration** : Réseau Haies / AFAC référentiel coût gestion juin 2024,
amendement Sénat novembre 2025.

**Estimation** : 0.5 jour.

---

## 19. Reformuler la croissance des haies comme proxy explicite

**Pourquoi reporté** : la constante `AnnualGrowthMetersPerHectare = 0.5`
dans `HedgerowGrowthRule` est présentée comme un "taux de croissance
naturelle des linéaires bocagers", ce qui est sémantiquement ambigu. Une
haie existante ne s'allonge pas linéairement — elle s'épaissit, se
densifie en biomasse, et comble ses discontinuités. Le 0,5 m/ha/an
représente un **proxy de densification fonctionnelle** sans équivalent
direct dans la littérature.

La fourchette AFAC pour la régénération naturelle dans des contextes
favorables est 0,2–0,4 m/ha/an, ce qui suggère que 0,5 est dans le haut
de fourchette (acceptable pour un bocage percheron bien géré, mais à
documenter honnêtement).

**Cible** :
- Renommer la constante en `AnnualDensificationProxyMetersPerHectare`
  ou ajouter un XML doc clair distinguant le proxy du taux linéaire réel.
- Recalibrer sur la fourchette AFAC 0,2–0,4 m/ha/an si un agronome
  valide que 0,5 est trop optimiste pour le scénario de référence.
- Documenter dans `DECISIONS.md` la distinction entre allongement et
  densification fonctionnelle.

**Calibration** : AFAC-Agroforesteries (fourchette régénération
naturelle), à confronter avec un référent agronome Perche.

**Estimation** : 0.3 jour (renommage + doc) + arbitrage agronome.

---

## 20. Recommandations préventives (anticipatives)

**Pourquoi reporté** : les 3 recommandations de la v1 (irrigation,
ReduceInputs, et PlantHedges dormante) sont toutes réactives — un seuil
est franchi, on alerte. Un Digital Twin de support à la décision propose
aussi des recommandations anticipatives, basées sur des tendances
détectées avant le franchissement de seuil. La v1 perd en richesse
pédagogique : la tech ne crie que quand ça casse, elle n'aide pas à
éviter que ça casse.

**Architecture cible** :
- Nouveau composant `TrendDetector` en Couche 2
  ([`Assets/_Project/02_Sensors/`](Assets/_Project/02_Sensors/)) qui
  maintient un ring-buffer de 60 jours par variable surveillée et calcule
  la pente glissante (régression linéaire ou simple delta).
- Nouvelle famille d'événements `TrendDetectedEvent` avec champs
  "variable observée", "horizon prédit", "confiance".
- Mappings préventifs candidats :
  - **Tendance nappe** : pente négative significative sur 60 jours
    convergeant vers le seuil drought 3.5 m → reco "renforcer couverture
    sol + couverts d'interculture" avant que la sécheresse soit
    caractérisée.
  - **Anomalie acoustique en formation** : mesure dans la zone d'alerte
    précoce (0.75–0.85, en deçà du seuil 0.7 de
    [`EventDetector`](Assets/_Project/02_Sensors/EventDetector.cs)) →
    reco "audit des pratiques + ralentissement progressif des intrants".
  - **Saisonnalité défavorable prévue** : si #12 implémenté, fenêtre de
    semis avec prévision météo défavorable → reco "décaler le semis de
    7–14 jours".

**Pré-requis** :
- Ring-buffer par variable (architecture commune avec #11 popup chart
  real-vs-shadow).
- #12 (saisonnalité météo) pour le 3e cas.

**Calibration** : protocoles d'alerte précoce Chambre d'agriculture
Normandie, RMT Sols et Territoires.

**Estimation** : 1–1.5 jour (capteur tendance + 2–3 cas + UI alerte
distincte des alertes réactives).

---

## 21. Levier diversification des cultures

**Pourquoi reporté** : le mix de cultures est figé à 70 % blé tendre /
30 % colza dans
[`CropYieldDynamicsRule`](Assets/_Project/01_SimulationCore/Rules/CropYieldDynamicsRule.cs).
L'utilisateur n'a aucun moyen de faire varier la diversité d'assolement,
alors que c'est l'une des trois voies d'accès à l'écorégime PAC (la voie
pratiques exige une diversification 4 cultures + légumineuses ≥ 4 % de
la SAU) et un outil majeur de résilience documenté par Solagro
Afterres2050 et INRAE rotation systems.

**Cible** :
- Nouveau paramètre dans
  [`ScenarioContext`](Assets/_Project/01_SimulationCore/Scenario/ScenarioContext.cs)
  : `CropDiversityIndex` (0 = monoculture, 1 = assolement diversifié
  4+ cultures avec légumineuses).
- Refonte de `CropYieldDynamicsRule` : remplacer le mix figé par un
  calcul dépendant de l'index. Une rotation diversifiée a un rendement
  moyen légèrement plus bas (−5 % à −10 %) mais une variance réduite et
  une meilleure résilience aux extrêmes climatiques.
- Effet économique : haute diversité ouvre la voie écorégime supérieur
  (46 → 63 €/ha PAC 2025) → impact direct sur la rentabilité intégrée.
- Effet biodiversité : haute diversité augmente `FaunaPopulation` via
  le couplage faune utile / habitat varié (lien futur avec #15).
- UI : slider continu dans le panneau de droite "Diversification de
  l'assolement".

**Calibration** : Solagro Afterres2050 (rotations diversifiées), INRAE
rotation systems (variance de rendement), écorégime PAC 2023–2027 voie
diversification (4 cultures minimum, légumineuses ≥ 4 % SAU).

**Estimation** : 1 jour.

---

## 22. Événement échec de plantation

**Pourquoi reporté** : dans la réalité, 30–50 % des plants meurent les
3 premières années (sécheresse à la reprise, broutage cerf/chevreuil,
défaut d'entretien estival). La recommandation `PlantHedges` (dormante
via #16) augmenterait la densité linéairement sans aléa, ce qui
sous-estime le coût et le risque réel de la replantation. Un événement
"échec" rendrait l'arbitrage agriculteur plus honnête : faut-il replanter
en plein été sec, y aller par cohortes progressives, ou intensifier
l'entretien la première année ?

**Architecture cible** :
- Nouvelle classe `PlantingCohort` (date de plantation, magnitude en
  m/ha, état vivant/échoué, conditions hydriques au moment du semis).
- Nouvelle règle `PlantingMortalityRule` qui calcule chaque jour la
  fraction de mortalité selon : âge de la cohorte (3 premières années
  critiques), profondeur de la nappe au moment de la plantation, valeur
  de `MaintenanceCost` comme proxy de l'effort d'entretien.
- Nouvel événement `PlantingFailureEvent` levé si > 30 % de mortalité
  sur une cohorte donnée (seuil à affiner par calibration).
- Nouvelle reco `CompletePlantingRecommendation` : compléter la
  plantation manquante, magnitude proportionnelle à la perte constatée.

**Pré-requis** :
- **#16 actif** : `PlantHedges` reco réactivée — sans plantation, rien
  à faire échouer.
- **#14 actif** : couplage sécheresse → mortalité plants, pour moduler
  la probabilité d'échec par l'état hydrique.
- **#18** : `MaintenanceCost` recalibré, pour que le coût de l'échec
  (réinvestissement + entretien renforcé) soit visible dans la
  rentabilité.
- **#9** : capital d'investissement, sinon l'échec n'a pas de coût
  représenté.

**Calibration** : PNR Perche (protocoles de plantation et taux de
reprise), AFAC-Agroforesteries (taux de reprise terrain : 50–80 % selon
essences et années climatiques), INRAE (mortalité juvénile des plants
ligneux).

**Estimation** : 1–1.5 jour une fois les pré-requis #16, #14, #18 et #9
levés.

---

## 23. Gestion de la mare (double usage du piézomètre + événement + reco)

**Pourquoi reporté** : la mare est présente dans la scène visuelle
(sprite, niveau d'eau dérivé indirectement de la piézométrie via
[`PondShaderBinding`](Assets/_Project/05_Presentation/Bindings/PondShaderBinding.cs))
et citée dans les sources (amphibiens, OFB / RMT Zones humides), mais
aucun événement ni recommandation ne lui est dédié en v1. La mare est
pourtant un micro-écosystème distinct qu'un agriculteur ou un PNR
surveille spécifiquement : assèchement estival, présence d'amphibiens en
période de reproduction.

Le piézomètre existant est déjà placé visuellement dans la mare et
configuré pour mesurer la nappe phréatique en v1. L'évolution prévue est
de lui faire jouer un **double usage** logique : mesure de la nappe ET
du niveau d'eau libre de la mare, ce qui est techniquement plausible avec
une sonde multiparamètres. **Aucun nouveau sprite ni nouveau capteur
visuel n'est à ajouter** — uniquement l'extension logique du capteur
existant et le câblage d'une nouvelle variable d'état + événement + reco.

**Cible** :
- **Extension logique du piézomètre** dans
  [`SensorPlacement_Default.asset`](Assets/_Project/Data/SensorPlacement_Default.asset)
  : ajouter une seconde variable observée `PondWaterLevelMeters` en plus
  de `WaterTableDepth`. Pas de nouveau GameObject, pas de nouveau sprite.
- **Nouvelle variable d'état** dans
  [`EcosystemModel`](Assets/_Project/01_SimulationCore/Model/EcosystemModel.cs)
  : `PondWaterLevelMeters` (m), dérivée de la nappe + des précipitations
  récentes, avec dynamique propre (forte sensibilité à
  l'évapotranspiration estivale). Baseline 0.8 m, plage 0–1.5 m.
- **Nouvelle règle** `PondDynamicsRule` : remplit avec pluie, vide avec
  évaporation modulée par température, plafonnée par la profondeur
  géologique de la cuvette.
- **Nouvel événement** `PondDryingOutEvent` dans
  [`Assets/_Project/02_Sensors/Events/`](Assets/_Project/02_Sensors/Events/)
  : déclenché si `PondWaterLevelMeters < 0.2 m` sur 14 jours consécutifs
  (cooldown plus court que drought — la mare est plus réactive que la
  nappe).
- **Nouvelle reco** `PondMaintenanceRecommendation` : curage + remise
  en eau via dérivation de fossé ou apport temporaire.
- **Effet sur biodiversité** : la mare contribue à `FaunaPopulation` via
  le facteur amphibiens — à isoler proprement dans la décomposition du
  composite (lien avec #15 refonte biodiversité).
- **Effet visuel** : le sprite mare existant est modulé par
  `PondWaterLevelMeters` directement (lien avec #6 qui mentionne déjà
  la propriété shader `_WaterLevel` dans
  [`S_Pond.shader`](Assets/_Project/05_Presentation/Scene/Shaders/S_Pond.shader)).

**Pré-requis** :
- Idéalement #15 (refonte biodiversité) pour isoler proprement la
  composante amphibiens du composite `FaunaPopulation`. Implémentable en
  standalone si on accepte que l'impact biodiversité transite par le
  scalaire existant.

**Calibration** : OFB / RMT Zones humides (seuils de mortalité
amphibiens selon profondeur résiduelle), PNR Perche (protocoles de
suivi des mares), Agence de l'Eau Seine-Normandie (limnologie des
petites masses d'eau stagnantes), Conservatoire d'espaces naturels
Normandie.

**Estimation** : 1 jour (variable d'état + règle + événement + reco +
câblage double usage capteur + binding visuel).

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
- Items 20–23 : issus de l'audit logique capteur → décision (audit de
  design post-publication v1, manques identifiés sur la palette de
  recommandations, l'instrumentation de la mare et la richesse des
  leviers utilisateur).
  - #20 (recommandations préventives) : palette réactive insuffisante.
  - #21 (diversification cultures) : levier manquant + écorégime
    supérieur non accessible.
  - #22 (échec plantation) : arrachage #16 sans risque = arbitrage
    faussé.
  - #23 (mare) : micro-écosystème présent visuellement, sans modèle
    ni chaîne capteur → événement → reco.

Tout item ajouté au backlog doit pointer vers la décision ou le
livrable d'origine pour ne pas perdre la traçabilité.
