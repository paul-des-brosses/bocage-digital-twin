# DECISIONS.md — Journal des décisions

Log des décisions de design prises pendant la phase d'exploration. Format
ADR (Architecture Decision Record) light. Une entrée = une décision
tranchée. À mettre à jour au fil du projet si une décision est révisée.

---

### 1. Sujet du projet : digital twin du bocage normand percheron

**Contexte** : choix d'un biome ou d'un objet de simulation cohérent avec
le profil portfolio (Creative Technology, R&D Ardanti) et accessible en
documentation.

**Décision** : digital twin d'un bocage normand percheron instrumenté.

**Raison** : sujet riche en services écosystémiques mesurables, ancrage
territorial fort (PNR du Perche), données publiques disponibles
(Solagro, INRAE, Efese, MAEC), pertinence agroécologique actuelle.

**Alternative écartée** : récif corallien instrumenté — trop éloigné du
contexte français, données moins accessibles, signal portfolio moins
distinctif.

---

### 2. Format visuel : 2D plan fixe minimaliste

**Contexte** : choisir un format compatible WebGL portfolio, lisible et
réalisable en temps contraint.

**Décision** : 2D plan fixe minimaliste, caméra strictement immobile.

**Raison** : maximise la lisibilité des indicateurs, évite la dérive
ludique, scope tenable, cohérent avec une UI de tableau de bord.

**Alternative écartée** : 3D top-down ou 2.5D — coût de production des
assets et complexité shader/perf disproportionnés pour un portfolio.

---

### 3. Style visuel : Charles Harper + A Short Hike + colombages percherons

**Contexte** : positionner le projet entre rigueur scientifique et
chaleur visuelle pour éviter le rendu "tableau de bord froid".

**Décision** : esprit Charles Harper (formes plates géométriques, palette
maîtrisée), chaleur de A Short Hike (douceur d'éclairage, ambiance
chaleureuse), inspiration architecture à colombages du Perche (palette
ocre-brun-vert sourd).

**Raison** : crédibilité naturaliste sans austérité, signature visuelle
distinctive en portfolio, ancrage territorial fort.

**Alternative écartée** : style high-tech propre (trop générique), pixel
art (trop ludique), photoréalisme (hors scope production).

---

### 4. Mode UI : dark mode éditorial scientifique

**Contexte** : choisir un mode d'interface cohérent avec l'identité
visuelle et le confort de lecture longue.

**Décision** : dark mode éditorial scientifique, validé après génération
d'image de référence.

**Raison** : cohérent avec l'esthétique d'observatoire / station de
recherche, contraste élevé pour la lisibilité des chiffres, distinctif en
portfolio.

**Alternative écartée** : light mode papier — moins distinctif, contraste
moindre sur les visualisations colorées.

---

### 5. Typographie : EB Garamond + JetBrains Mono

**Contexte** : asseoir l'identité éditoriale et garantir la lisibilité
chiffrée.

**Décision** : EB Garamond pour titres et labels, JetBrains Mono (ou IBM
Plex Mono) pour valeurs chiffrées.

**Raison** : Garamond évoque le scientifique éditorial sérieux ; mono
moderne pour la précision numérique. Couple lisible et distinctif.

**Alternative écartée** : sans-serif moderne uniforme — visuellement
banal, pas de hiérarchie typographique.

---

### 6. Cadre idéologique : techno-optimisme modéré + réalisme agroécologique

**Contexte** : éviter le piège politique d'un projet polarisant tout en
défendant une thèse claire.

**Décision** : techno-optimisme modéré combiné à un réalisme
agroécologique. Pas de "croissance verte" ni de "décroissance" assumés.

**Raison** : thèse défendable scientifiquement, audience portfolio large,
neutralité politique sans être tiède.

**Alternative écartée** : posture militante (pro ou anti) — clive
inutilement l'audience portfolio.

---

### 7. Niveau de calibration : moyen, chiffres réels Solagro/INRAE/Efese/MAEC

**Contexte** : équilibrer rigueur scientifique et faisabilité de
production.

**Décision** : calibration de niveau moyen, basée sur chiffres publics
réels. Pas de forçage de l'issue.

**Raison** : crédibilité auprès d'un agroécologue ou d'un agent PNR sans
viser une publication scientifique.

**Alternative écartée** : calibration ultra-rigoureuse type modèle
INRAE — hors scope ; calibration purement inventée — perte de crédibilité.

---

### 8. Indicateurs éco et écolo en parallèle, jamais opposés

**Contexte** : risque pédagogique d'opposer économie et écologie de
manière manichéenne.

**Décision** : indicateurs éco et écolo affichés en parallèle. Rentabilité
intégrée comme KPI central (€/ha/an incluant services écosystémiques
monétisés).

**Raison** : reflète la thèse de convergence possible, évite la
caricature, pédagogiquement plus juste.

**Alternative écartée** : afficher un seul axe "performance globale" —
masque les arbitrages.

---

### 9. Onglet comparatif avec/sans tech : simulation fantôme parallèle

**Contexte** : comment démontrer l'apport de l'instrumentation sans le
postuler.

**Décision** : simulation fantôme parallèle, mêmes seeds et mêmes inputs,
sans application des actions tech.

**Raison** : démonstration honnête (différence due exclusivement aux
actions), reproductibilité, alignement avec la thèse du projet.

**Alternative écartée** : comparaison avec valeurs codées en dur — non
crédible.

**Précisé par ADR #58 (chantier E8, 2026-06-04)** : le cadrage « mêmes
inputs » est affiné en contrefactuel à baseline gelée — les paramètres
exogènes (climat, MAEC, PSE) sont partagés, mais les quatre leviers de
décision agriculteur sont gelés à leur valeur de lancement.

---

### 10. Hiérarchie KPIs en 3 niveaux

**Contexte** : densité d'information à organiser sans noyer l'utilisateur.

**Décision** : 5 Hero KPIs (densité haies, biodiversité composite, nappe
phréatique, rentabilité intégrée, delta tech), 3 panneaux Niveau B
(Biodiversité, Climat & ressources, Économie), popovers Niveau C sur
clic capteur.

**Raison** : structure progressive de l'information, lecture rapide
possible, profondeur disponible à la demande.

**Alternative écartée** : tableau plat exhaustif — illisible.

---

### 11. Temporalité : simulation continue, x1/x10/skip, pas de cycle jour/nuit

**Contexte** : choisir un modèle de temps cohérent avec les phénomènes
observés.

**Décision** : simulation continue, play/pause, vitesses x1 et x10, skip
to end au-delà. Pause maintient les animations de scène. Pas de cycle
jour/nuit. Saisons gérées via shaders pilotés par la météo simulée, pas
par calendrier.

**Raison** : phénomènes observés (croissance haies, dynamique nappe) à
échelle pluriannuelle ; cycle jour/nuit hors scope et inutile.

**Alternative écartée** : tick discret par mois — perte de granularité
sur les événements rapides.

---

### 12. Modification des presets : transition interpolée 7-14 jours

**Contexte** : éviter les sauts visuels brutaux quand l'utilisateur
modifie un preset.

**Décision** : transition interpolée sur 7-14 jours simulés via
`TransitioningParameter<T>`.

**Raison** : crédibilité physique (les paramètres écosystémiques ne
sautent pas), confort visuel.

**Alternative écartée** : application immédiate — peu crédible et
visuellement abrupt.

---

### 13. Primauté du capteur : aucun visuel piloté par le calendrier

**Contexte** : tentation de scénariser des effets (feuilles d'automne,
neige) pour ambiancer.

**Décision** : aucun élément visuel piloté par le calendrier. Tout dérive
d'une mesure ou d'une variable du modèle, traçable jusqu'à un capteur ou
un calcul.

**Raison** : c'est ce qui distingue un digital twin d'un jeu vidéo.
Garantie d'honnêteté du démonstrateur.

**Alternative écartée** : effets décoratifs scriptés — perte de crédibilité
projet.

---

### 14. Contrat utilisateur double casquette : Scénario + Décisions

**Contexte** : clarifier ce que fait l'utilisateur.

**Décision** : casquette **Scénario** (curseurs presets, permanente) +
casquette **Décisions de gestion** (recommandations à arbitrer, apparaît
sur événements détectés).

**Raison** : sépare clairement le réglage de contexte (passif) et l'acte
de gestion (actif). Pédagogiquement net.

**Alternative écartée** : tout en un seul panneau — confond paramétrage
et action.

---

### 15. Module de décision : implémentation riche

**Contexte** : niveau d'ambition sur le moteur de décision.

**Décision** : implémentation riche, avec incertitudes (distributions),
horizons multiples (court / moyen / long terme), contraste choix
utilisateur vs choix optimisé.

**Raison** : signal portfolio fort, démontre une vraie modélisation de
décision en environnement incertain.

**Alternative écartée** : règles simples si/alors — banal, peu
distinctif.

---

### 16. Caméra : plan strictement fixe

**Contexte** : tentation d'ajouter du parallax ou un léger zoom.

**Décision** : plan strictement fixe, aucun parallax, aucun zoom.

**Raison** : cohérent avec un format de tableau de bord, simplifie la
production, lisibilité maximale des positions de capteurs.

**Alternative écartée** : parallax léger — gain visuel marginal, coût en
complexité d'organisation des sprites.

---

### 17. Plateforme : desktop-only assumé

**Contexte** : viser ou non le mobile.

**Décision** : desktop only. Pas de responsive mobile, pas de tactile.
Bandeau d'avertissement si fenêtre < 1280 px.

**Raison** : densité d'information incompatible avec le mobile, scope
tenable, cible portfolio (recruteurs sur desktop).

**Alternative écartée** : responsive mobile — coût production triple
sans gain portfolio.

---

### 18. Lien scène ↔ data : hover synchronisé minimap/scène

**Contexte** : comment relier les capteurs visibles dans la scène à leur
représentation sur la minimap.

**Décision** : hover synchronisé minimap ↔ scène. Pas de clic depuis la
scène (pour préserver la lecture immersive).

**Raison** : interaction lisible et non intrusive ; la minimap reste le
point d'entrée actif.

**Alternative écartée** : clic direct sur sprite scène — bruit
interactif, ambiguïté avec animations de faune.

---

### 19. Onboarding : tooltips contextuels, pas d'intro textuelle

**Contexte** : comment expliquer l'interface sans intro intrusive.

**Décision** : tooltips contextuels en Garamond italique sur hover, pas
d'intro textuelle au lancement. Noms de panneaux extrêmement explicites.

**Raison** : démarrage instantané, exploration guidée par survol, pas de
modal bloquant.

**Alternative écartée** : tutoriel pas-à-pas — coût de production élevé,
intrusif pour l'audience portfolio.

---

### 20. Architecture en 5 couches

**Contexte** : structure du code pour un projet Unity testable et
maintenable.

**Décision** : 5 couches (SimulationCore / Sensors / Decision /
Indicators / Presentation). Asmdef par couche, références strictes vers
les couches inférieures uniquement.

**Raison** : testabilité de la Couche 1 en pure C#, séparation Unity /
métier nette, signal portfolio fort sur l'architecture logicielle.

**Alternative écartée** : architecture monolithique MonoBehaviour —
intestable, signal portfolio faible.

---

### 21. Pattern de communication : ScriptableObjects observables + EventBus

**Contexte** : choisir un pattern Unity pour la communication entre
couches.

**Décision** : ScriptableObjects observables (event `OnChanged`) pour
indicateurs et état persistant ; EventBus statique pour événements
ponctuels (chalara détecté, sécheresse déclenchée, etc.).

**Raison** : découplage fort, inspectable dans l'éditeur, testable, idiom
Unity reconnaissable.

**Alternative écartée** : injection de dépendances (Zenject/VContainer) —
sur-ingéniérie pour ce scope.

---

### 22. Tick rate : 1 tick = 1 jour simulé

**Contexte** : granularité temporelle de la simulation.

**Décision** : 1 tick = 1 jour simulé.

**Raison** : compromis entre granularité (suffisante pour événements
quotidiens : pluies, sondages capteurs) et coût computationnel.

**Alternative écartée** : tick horaire — coût élevé sans gain pour les
phénomènes observés.

---

### 23. Seed déterministe avec sous-seeds dérivés par hash

**Contexte** : garantir la reproductibilité et la cohérence simulation
fantôme.

**Décision** : seed maître au démarrage, sous-seeds dérivés par hash pour
chaque sous-système (météo, faune, capteurs, événements).

**Raison** : reproductibilité totale, isolation des sources d'aléa,
nécessaire pour la comparaison real run / shadow run.

**Alternative écartée** : un seul `Random` global — impossibilité de
comparer real et shadow run.

---

### 24. Simulation fantôme : interface ISimulationRun, deux instances

**Contexte** : implémentation technique de la comparaison
avec/sans tech.

**Décision** : interface `ISimulationRun`, deux instances avec flag
`applyTechActions` (true / false). Mêmes seeds, mêmes inputs.

**Raison** : implémentation propre, divergence garantie uniquement par
les actions tech.

**Alternative écartée** : duplication de logique — fragile, source de
bugs.

**Remplacé par ADR #58 (chantier E8, 2026-06-04)** : `ISimulationRun` /
`applyTechActions` n'ont jamais été construits ; le shadow utilise un
second `SimulationEngine` concret + un `ScenarioContext` frozen-baseline
(`TickWithoutAdvancingScenario`).

---

### 25. Une seule scène Unity (Main), 7 racines préfixées `_`

**Contexte** : organisation de la hiérarchie Unity.

**Décision** : scène unique `Main`, 7 racines préfixées `_` (`_Bootstrap`,
`_Camera`, `_Scene_Visual`, `_Scene_Overlays`, `_UI_Canvas`, `_Audio`,
`_Debug`).

**Raison** : simplicité, hiérarchie lisible, isolation visuelle des
domaines dans l'éditeur.

**Alternative écartée** : multi-scène additif — sur-ingéniérie pour ce
scope.

---

### 26. Persistance : PlayerPrefs minimal

**Contexte** : que sauvegarder entre sessions.

**Décision** : PlayerPrefs minimal — dernière configuration de presets
et vitesse choisie. Rien d'autre.

**Raison** : démonstrateur portfolio, pas de profil utilisateur, pas de
sauvegarde de session.

**Alternative écartée** : sauvegarde JSON de session — hors scope.

---

### 27. Logging : SimLogger 3 niveaux, pas de Debug.Log direct

**Contexte** : maîtriser le bruit de log et le coût en runtime WebGL.

**Décision** : `SimLogger` à 3 niveaux (`DebugLog`, `SimulationLog`,
`UserActionLog`). Pas de `Debug.Log` direct dans le code applicatif.

**Raison** : filtrage centralisé, désactivation possible en build, signal
portfolio sur la rigueur d'instrumentation.

**Alternative écartée** : `Debug.Log` partout — bruit, coût runtime,
incontrôlable.

---

### 28. Audio : aucun

**Contexte** : faut-il intégrer du son.

**Décision** : aucun audio. Aucune musique, aucun bruitage, aucun son
d'ambiance, aucun feedback UI sonore.

**Raison** : le projet est une station d'observation silencieuse ; éviter
le coût production audio ; éviter les pièges WebGL audio.

**Alternative écartée** : ambiance sonore légère — coût production +
risques WebGL (autoplay policies) sans gain portfolio.

---

### 29. Pipeline assets : Nanobanana + ip-adapter + post-traitement Python

**Contexte** : produire 15 sprites uniques cohérents en style.

**Décision** : Nanobanana avec ip-adapter style reference (image de
référence stylistique générée en premier), post-traitement Python
(palette quantization, alpha cleanup, normalisation).

**Raison** : cohérence stylistique inter-sprites, contrôle de la palette,
itération rapide.

**Alternative écartée** : achat asset pack — perte d'identité visuelle ;
dessin manuel — hors scope temps.

---

### 30. Stratégie portfolio Position C : usage IA assumé sobrement

**Contexte** : comment positionner l'usage des outils IA en portfolio.

**Décision** : usage assumé sobrement dans le README (section "Method"),
en distinguant ce qui est IA-assisté (code, sprites) et ce qui est
décision humaine (architecture, calibration scientifique, design).

**Raison** : honnêteté professionnelle, signal de maturité, pas de cache
ni de survalorisation.

**Alternative écartée** : ne pas mentionner — malhonnête et facilement
détectable.

---

### 31. README en anglais

**Contexte** : langue du README.

**Décision** : anglais.

**Raison** : audience portfolio internationale (recruteurs, github
trending, équipes anglophones).

**Alternative écartée** : français — limite l'audience portfolio.

---

### 32. Pas de mention publique du temps de réalisation

**Contexte** : faut-il indiquer "réalisé en X semaines" en portfolio.

**Décision** : pas de mention du temps de réalisation publique.

**Raison** : la valeur portfolio est dans le résultat, pas dans le
temps ; le temps est trompeur (IA-assisté vs solo) et invite à des
comparaisons hors-sujet.

**Alternative écartée** : mention explicite — biaise la lecture.

---

### 33. Workflow Git : Claude Code exécute (révisé)

**Contexte** : qui exécute les commandes Git.

**Décision (révisée le 2026-04-25)** : Claude Code exécute lui-même
`git add`, `git commit`, `git push` au format Conventional Commits,
aux moments opportuns. L'utilisateur garde un pouvoir d'intervention
permanent (stop, amend, revert, no-push). Les opérations risquées
(force push, `reset --hard`, rewrite d'historique poussé) restent à
valider explicitement.

**Décision initiale (rejetée le 2026-04-25)** : l'utilisateur exécute
toutes les commandes Git, Claude Code propose seulement les messages.
Constat à l'usage : friction conversationnelle élevée, chaque palier
nécessitait un copier-coller manuel.

**Raison** : fluidité de la session de production. L'historique
reste propre tant que les messages restent rigoureux et que les
moments de commit sont bien choisis. Le pouvoir d'intervention
permanent suffit pour rattraper toute dérive.

**Alternative écartée** : revenir à la décision initiale au cas par
cas — incohérent et inutilement coûteux.

---

### 34. Roadmap en 10 étapes verticales avec livrable démontrable

**Contexte** : découpage du projet pour piloter la production.

**Décision** : 10 étapes verticales, chacune avec un livrable démontrable
(slice de bout en bout, pas de couche horizontale isolée).

**Raison** : permet de couper proprement à n'importe quelle étape,
chaque palier est une "version montrable", motivant.

**Alternative écartée** : découpage horizontal par couche — risque de
livrer 80 % de couches sans démo fonctionnelle.

---

### 36. Composition de scène data-driven via ScriptableObject

**Contexte** : à l'Étape 4, choix entre composer la scène à la main dans
l'éditeur Unity (drag & drop des sprites) ou la générer depuis un
ScriptableObject lu au boot.

**Décision** : composition data-driven. `SceneCompositionDefinition`
(ScriptableObject) liste les `ScenicElement` (sprite, position, scale,
sorting layer, ordre). `SceneAssembler` (MonoBehaviour) instancie tout
sous `_Scene_Visual` au Awake.

**Raison** : signal portfolio fort (séparation data/présentation,
reproductibilité), permet plus tard des variantes de composition
(preset été/hiver/sécheresse) sans toucher la scène, aligné avec la
thèse digital twin (la scène est une lecture de données, pas une mise
en scène). Coût supplémentaire ~2× le code, jugé raisonnable pour une
dizaine d'éléments de décor.

**Alternative écartée** : composition manuelle dans la scène Unity —
plus rapide mais sans valeur architecturale, et impose de modifier la
scène à chaque variation.

---

### 37. Shaders : Shader Graph pour tous les shaders runtime

**Contexte** : choix entre HLSL pur (`.shader`) et Shader Graph
(`.shadergraph`) pour les shaders du projet (ciel, prairie, haies,
mare).

**Décision** : Shader Graph pour l'ensemble des shaders runtime
(`SG_Sky`, `SG_Hedgerow`, `SG_Pond`, `SG_Meadow` à venir).

**Raison** : preview live dans l'éditeur (itération visuelle x10 plus
rapide quand l'effet n'est pas trivial), maintenabilité par un
non-spécialiste graphique sur la durée du portfolio, absorption de la
plomberie URP 2D version-spécifique. Pour le ciel seul l'argument est
marginal, mais l'uniformité du pipeline shaders vaut mieux que
l'optimum local.

**Conséquence opérationnelle** : Claude Code scaffolde les Shader
Graphs en spécifiant le contrat (nom des propriétés exposées,
structure du graphe). L'utilisateur câble les nœuds dans l'éditeur
Unity à partir des instructions pas-à-pas — un fichier
`.shadergraph` étant du YAML auto-généré avec GUIDs, son authoring
hors-éditeur n'est pas fiable.

**Alternative écartée** : HLSL pur — gain négligeable sur les shaders
simples, perte sur les shaders complexes.

---

### 38. Sorting layers de la scène 2D

**Contexte** : ordre de rendu des sprites dans la scène 2D.

**Décision** : 7 sorting layers déclarés dans `ProjectSettings/TagManager.asset`,
du fond vers l'avant : `Sky`, `Background`, `Midground`, `Foreground`,
`Sensors`, `Fauna`, `FX`. Le layer `Default` est conservé pour les
objets non visuels.

**Raison** : alignement direct sur la sémantique de la scène
(catégories Charles Harper / A Short Hike), élimine les conflits d'ordre
Z intra-catégorie, simplifie l'authoring des `ScenicElement` dans le
`SceneCompositionDefinition`.

**Alternative écartée** : un seul layer `Default` avec gestion fine
par `sortingOrder` int — fragile et illisible.

---

### 35. Pas d'audio, pas de mobile, pas de modal intrusif

**Contexte** : éléments à exclure explicitement du scope.

**Décision** : pas d'audio (cf #28), pas de support mobile (cf #17), pas
de modal intrusif (intro, tutoriel, dialogue bloquant).

**Raison** : focus, scope tenable, cohérence avec une station
d'observation silencieuse.

**Alternative écartée** : "on verra plus tard" — amène scope creep.

---

### 39. Ordre des Hero KPIs dans le hero strip (pyramide cause → effet)

**Contexte** : 5 Hero KPIs sont prévus dans le dashboard
(`HedgerowDensity`, `WaterTable`, `BiodiversityComposite`,
`IntegratedProfitability`, `TechDelta`). L'ordre d'affichage de
gauche à droite raconte une histoire au lecteur.

**Décision** : ordre adopté `Haies → Nappe → Biodiversité →
Rentabilité → Delta tech`. Substrat physique (haies, eau) à gauche,
intégrateur écologique au centre, valorisation économique à droite,
arbitrage méta tout à droite.

**Raison** : lecture pédagogique d'un digital twin agro-écologique.
On lit la chaîne causale du concret au méta : structure du paysage →
ressource physique → effet écosystémique → effet économique →
"est-ce que la tech aide ?". Cohérent avec la thèse du projet
(test honnête de la convergence éco/écolo, cf §1 CLAUDE.md).

**Alternatives écartées** :
- *Honnêtes à gauche, stubs à droite* (Haies / Nappe / Rentabilité /
  Biodiv / Delta tech) : sépare arbitrairement biodiv et nappe qui
  sont conceptuellement liées.
- *Par poids dans le récit* (Delta tech en premier) : afficher en
  pole position un KPI qui vaut 0 jusqu'à l'Étape 8 est un mauvais
  signal visuel pour le portfolio.

---

### 40. Refus des Hero KPIs en stub — différer jusqu'à existence des variables d'état

**Contexte** : à la sous-étape 6a, 3 des 5 Hero KPIs prévus
(`Biodiversity`, `Profitability`, `TechDelta`) n'ont pas de variable
d'état correspondante dans `EcosystemModel`. Tentation initiale :
les implémenter comme formules dérivées des 2 variables existantes
(`HedgerowDensity`, `WaterTableDepth`) pour "câbler le pattern".

**Décision** : refus des stubs dérivés. Les 3 indicateurs et leurs
containers `RC_*` ne sont **pas** créés tant que les variables
sous-jacentes n'existent pas. À 6b les 3 cartouches correspondantes
afficheront un placeholder visuel "à venir" avec un libellé qui
indique l'étape où le KPI sera branché honnêtement.

**Raison** : le principe de primauté du capteur (CLAUDE.md §9) exige
que toute valeur affichée soit traçable jusqu'à une variable du
modèle. Une formule arbitraire `0.65 × hedgerowNorm + 0.35 ×
waterNorm` qu'on appellerait "biodiversité composite" *est* de la
donnée inventée, même si elle est déterministe. Un portfolio sur la
thèse "test honnête de la convergence éco/écolo" ne peut pas
afficher des chiffres de biodiversité, rentabilité et delta tech qui
ne reposent sur rien.

**Conséquence sur la roadmap** :
- `BiodiversityComposite` → arrive à l'Étape 8 (faune & shadow run :
  l'ajout de `FaunaPopulation` au modèle débloque un agrégat
  honnête).
- `IntegratedProfitability` → arrive à l'Étape 7 (économie : ajout
  de `CropYield`, `InputCost`, `MaintenanceCost`).
- `TechDelta` → arrive à l'Étape 8 (shadow run câblée, l'agrégat
  est calculable sur (real − shadow)). **Précisé par ADR #59
  (chantier E8)** : le KPI est une valeur NET cumulée en €/ha, pas
  instantanée.

**Alternatives écartées** :
- *Stubs câblés mais signalés visuellement* : compromis tentant
  mais on aurait quand même affiché des chiffres faux. Le badge
  "stub" sur la cartouche aurait été un cache-misère.
- *Étendre EcosystemModel maintenant* : gonfle l'Étape 6 de
  ~30-50 % et empiète sur les Étapes 7-8 prévues pour ce travail.

---

### 41. Shaders mare et prairie en HLSL plutôt que Shader Graph (révision partielle du #37)

**Contexte** : à la sub-étape 9α (livrable #4 de l'Étape 9), il fallait
livrer deux nouveaux shaders runtime : `SG_Pond` (mare pilotée par la
nappe) et `SG_Meadow` (prairie pilotée par l'humidité). La décision
#37 disait Shader Graph pour tous les shaders runtime.

**Décision** : déviation locale du #37 — `S_Pond.shader` et
`S_Meadow.shader` sont écrits en HLSL pur (`.shader`). `SG_Sky` et
`SG_Hedgerow` restent en Shader Graph et ne sont pas re-générés.

**Raison** :
- Les deux shaders en question sont simples (un lerp de couleur piloté
  par un float `[0,1]`). Le bénéfice "preview live" du SG est marginal
  ici.
- Authorer un `.shadergraph` à la main est impraticable (1500 lignes
  de YAML avec GUIDs internes), et c'est précisément ce que CLAUDE.md
  §2 demande à Claude Code de faire. Un `.shader` HLSL équivalent fait
  60–80 lignes lisibles, versionables, modifiables sans ouvrir Unity.
- Conséquence pour la suite : la couche binding consomme la même
  interface (`MaterialPropertyBlock` sur un float), donc passer
  ultérieurement à un Shader Graph est non bloquant (item backlog).

**Conséquence opérationnelle** :
- L'utilisateur ne crée plus le shader graph dans Unity pour la mare
  et la prairie ; les `.shader` sont importés tels quels.
- Si on veut un effet plus avancé plus tard (rides sur la mare,
  variation florale sur la prairie), on peut soit étendre les
  `.shader` en HLSL, soit refactoriser vers un `.shadergraph` en
  reprenant la même interface de propriétés. Documenté dans
  `BACKLOG.md`.

**Alternative écartée** : tenir le #37 strictement et demander à
l'utilisateur de créer manuellement les deux Shader Graphs depuis
zéro — ralentit la livraison de l'Étape 9 pour un gain visuel nul
au format actuel.

---

### 42. Hedgerow health proxy dérivé en Couche 4, pas variable d'état

**Contexte** : à la sub-étape 9β, on voulait moduler les sprites de
haies par un canal `_HealthT` représentant la "santé" du linéaire.
Tentation initiale : ajouter une propriété `HedgerowHealth` à
`EcosystemModel` avec des règles biophysiques de mise à jour
(chalara, sécheresse, recovery saisonnier, etc.).

**Décision** : `HedgerowHealth` n'est PAS une variable d'état. Elle
est calculée à la volée par `HedgerowHealthIndicator` (Couche 4) en
agrégeant la densité courante et les événements actifs de l'EventLog
(chalara récent, sécheresse récente) dans une fenêtre glissante de
60 jours.

**Raison** :
- Le principe de primauté du capteur (CLAUDE.md §9) n'exige pas qu'un
  visuel soit dérivé d'une variable d'état dédiée — il exige qu'il
  soit dérivé d'une mesure ou d'un calcul du modèle traçable. Une
  agrégation déterministe d'EventLog + state existant remplit ce
  contrat.
- Ajouter une variable d'état force des règles de dynamique
  artificielles (taux de récupération, couplage croisé) sans
  bénéfice pour le moteur de décision : la santé est une lecture, pas
  un levier.
- Garder la surface du modèle minimale facilite les tests et la
  reprise du projet pour ajouter de meilleurs effets visuels en
  backlog.

**Conséquence opérationnelle** :
- Le shader haies (`SG_Hedgerow`) doit lire `_HealthT` quand il sera
  étendu — entrée backlog "SG_Hedgerow healthT node". En attendant,
  le binding pousse silencieusement la valeur ; Unity ignore les
  propriétés non déclarées par le shader.
- Si une analyse plus fine s'impose un jour (saisons sèches
  cumulatives, fragmentation du linéaire), on pourra promouvoir
  `HedgerowHealth` en variable d'état sans casser l'API du binding.

**Alternative écartée** : variable d'état `HedgerowHealth` mise à
jour par une `HedgeHealthDynamicsRule` — surdimensionné pour le
besoin actuel, alourdit le modèle.

---

### 43. AutoAction `ReduceInputs` applique son effet directement sur le modèle réel, pas via le scénario partagé

**Remplacé par ADR #58 (chantier E8, 2026-06-04)** : la prémisse du
scénario partagé ne tient plus (shadow frozen-baseline) ; `ReduceInputs`
abaisse désormais `ScenarioContext.InputIntensityFactor` (changement de
pratique, transition §15). Les nudges +0.05 `FaunaPopulation` / −200
`InputCost` et le canal `RealRunTechAdjustment` proposé sont abandonnés.

**Contexte** : à la sub-étape 8c.3, l'auto-action `ReduceInputs`
(consommée par la recommandation du même nom + le bouton manuel
homonyme) doit traduire un arbitrage agriculteur « réduire les
intrants ponctuellement » en effet mécanique sur l'état simulé. La
voie naturelle serait de **baisser
`ScenarioContext.InputIntensityFactor`** : c'est le canal scénario
prévu pour modeler l'intensité des pratiques agricoles, et tout
l'aval (CropYieldDynamicsRule, InputCostDynamicsRule,
FaunaDynamicsRule) le consomme déjà.

**Tension architecturale** : le `ScenarioContext` est **partagé par
référence entre la run réelle et la shadow run**
(cf. `ShadowSimulationRunner` qui passe la même instance pour
garantir la non-divergence due au scénario). Si l'auto-action
modifiait `InputIntensityFactor`, la shadow run subirait
mécaniquement le même changement, et le KPI TechDelta — défini
comme « écart de rentabilité entre real et shadow » — s'annulerait
de fait. Le shadow run cesserait alors d'être le « scénario sans
décisions tech » que la thèse du DT prétend mesurer.

**Décision** : `AutoActionPipeline.ApplyOne` pour `ReduceInputs`
n'altère **pas** le `ScenarioContext`. Elle injecte ses effets
directement sur les variables d'état d'`EcosystemModel` du run
réel :
- `+0.05 × ratio` sur `FaunaPopulation` (boost ponctuel insectes)
- `−200 × ratio €/ha/an` sur `InputCost` (économie immédiate
  intrants évités)

Le `ratio` étant la magnitude utilisateur divisée par la valeur
de référence (`ReduceInputsRecommendation.IntensityCutPerStep`).
La shadow run, qui partage le scénario mais a son propre
`EcosystemModel`, n'est pas touchée → la divergence est capturée
par TechDelta.

**Raison** :
- Conserver la sémantique du shadow run comme « jumeau sans
  décisions tech » est non négociable pour la crédibilité du KPI
  central de l'étape 8.
- L'effet visé (boost faune + baisse coût) est sourcé : IPBES 2019
  (rebound faune après cessation pesticides), CIVAM grandes
  cultures (économies d'intrants conventionnels).
- L'alternative « cloner le ScenarioContext et baisser
  l'`InputIntensityFactor` sur la copie réelle uniquement » casse
  l'invariant d'unicité du scénario partagé documenté en
  ARCHITECTURE.md et impose une dérive de signatures à travers
  toute la pile.

**Conséquence opérationnelle** :
- L'effet sur la rentabilité passe par `InputCost` plutôt que par
  l'enchaînement scénarique. C'est une approximation : le vrai
  effet « réduction intensité » se propagerait aussi via
  `CropYieldDynamicsRule` (rendement légèrement abaissé) et via
  les coûts récurrents des années suivantes. Ici la baisse est
  one-shot ponctuelle sur la variable d'état.
- Limitation assumée : si l'utilisateur empile plusieurs auto-actions
  `ReduceInputs`, l'`InputCost` peut descendre arbitrairement bas
  (clamp à 0). La règle économique le rattrape sur les ticks
  suivants en tirant vers la cible scénario, mais le pic transitoire
  est un artéfact connu.
- Documenté dans le XML doc d'`AutoActionPipeline.ApplyOne` et
  rappelé dans le commentaire de classe.

**Chemin de sortie (post-MVP)** : introduire un canal d'ajustement
spécifique au run réel, du type
`EcosystemModel.RealRunTechAdjustment` (vecteur structuré, par
exemple `{ inputIntensityDelta, hedgeDensityDelta, … }`), que les
règles biophysiques consultent en plus du scénario partagé. La
shadow run l'ignore. `ReduceInputs` modifie alors un delta
sémantiquement clair (`inputIntensityDelta -= 0.2`) qui propage
proprement via les règles existantes. Estimation : 0.5–1 jour de
refactor, à arbitrer post-publication ; couvre aussi l'item
BACKLOG #9 (capital d'investissement) qui souffre d'une tension
similaire.

**Alternatives écartées** :
- *Modifier `InputIntensityFactor` du scénario partagé* : casse
  TechDelta (la shadow voit la même baisse).
- *Cloner `ScenarioContext` pour donner à chaque run le sien et
  ne baisser l'intensité que sur le clone réel* : viole l'invariant
  d'unicité du scénario (ARCHITECTURE.md §3 — un seul `ScenarioContext`
  par session, source de vérité unique pour les leviers
  agriculteur/cadre). Pollue les signatures de la chaîne sensor
  → recommendation → outcome avec une notion de « scenario contexte
  appartenant à qui ».
- *Repousser `ReduceInputs` au backlog jusqu'à ce que le canal
  `RealRunTechAdjustment` existe* : prive l'Étape 8 d'une des trois
  recos honnêtes qui démontrent la chaîne capteur → reco → impact,
  et donc d'un quart de sa valeur démonstrative.

---

### 44. Sémantique d'arbitrage des recommandations : Valider / Voir plus tard / Ignorer + verdict Superseded

**Contexte** : à la sub-étape 10a, l'audit a identifié deux frictions
sur la popup décision. (1) Cliquer **Ignorer** sur une reco
récurrente ne suffisait pas — l'`EventDetector` rebatissait la même
détection 30 jours plus tard et la popup repopait en boucle.
(2) Inversement, **Voir plus tard** sur N occurrences successives
d'un même type accumulait N entrées dans l'historique, le bouton
« Recommandations en cours (12) » devenant rapidement bruyant.

**Décision** : trois verbes utilisateur, trois sémantiques claires,
un quatrième verdict système pour borner l'historique.

**Verdicts utilisateur (trois boutons popup)** :

- **`Valider`** → verdict `Accepted`. L'auto-action est appliquée
  sur le modèle réel (pas sur le shadow). Le type de la reco est
  **retiré** du set d'ignore session — la prochaine occurrence du
  même type fera popup à nouveau, parce que l'utilisateur a montré
  qu'il s'engageait activement sur cette catégorie de décision.
- **`Voir plus tard`** → verdict reste `Pending`. La reco est
  ajoutée à un set `_skippedRecommendationIds` (clé : id
  d'instance) côté `DecisionPopupBinding` qui empêche son
  auto-popup pour la session. L'utilisateur peut la ré-ouvrir
  depuis le bouton historique. Une **nouvelle** instance du même
  type (event id différent) ne sera pas affectée — son propre
  auto-popup déclenchera normalement.
- **`Ignorer`** → verdict `Rejected`. **Le TYPE entier** de la reco
  est ajouté à `_ignoredRecommendationTypes` pour la session. Toute
  nouvelle reco dont l'id commence par le même préfixe (avant le
  `#`) est silencieusement skippée à l'auto-popup. Elle reste
  visible dans l'historique pour revisit, mais n'interrompt plus la
  simulation.

**Verdict système (auto-marqué dans le journal)** :

- **`Superseded`** → marqué automatiquement par `DecisionJournal.Append`
  quand une **nouvelle** reco arrive et qu'une `Pending` du même
  type est déjà dans le journal. L'ancienne devient `Superseded`,
  la nouvelle prend sa place comme seule `Pending` de ce type.
  Audit conservé (les entrées Superseded restent dans `Entries`),
  mais `PendingEntries` n'expose que la dernière → la liste
  historique est bornée à 1 entrée Pending par type.

**Conséquences** :

- Au plus **1 Pending par type** à un instant donné, quelle que
  soit la durée du run.
- Les `Accepted` et `Rejected` ne sont JAMAIS touchés par la
  supersession — le trail d'arbitrage utilisateur est intact pour
  un futur `SessionReporter` (jamais construit — BACKLOG #4).
- Les manipulations de set côté `DecisionPopupBinding` sont
  in-memory, perdues à la fin de la session — pas de persistance
  PlayerPrefs (CLAUDE.md §16). Une nouvelle session repart avec une
  liste vierge des types ignorés / différés.

**Raison de la double couche (journal + binding)** :

Le journal est l'autorité **modèle** (verdicts persistants pour
audit) ; les sets binding sont la couche **UX** (skipping
d'auto-popup pour ne pas saouler). Les deux sont indépendants :
- Tu peux ignorer une reco via Ignorer → le journal sait qu'elle
  est Rejected, l'auto-popup la skip via le set type.
- Tu peux revisiter via Examiner dans l'historique → la popup
  apparaît même si le type est dans le set ignore (override
  explicite par action utilisateur).
- Tu peux la re-Valider → journal passe à Accepted, set ignore
  vidé pour ce type, prochain event fera popup.

**Alternatives écartées** :
- *Pas de supersession, on accepte que l'historique grossisse* :
  bouton « Recommandations en cours (47) » illisible à un mois de
  run sim continu. Rejette aussi l'esprit MVP.
- *Marquer la nouvelle reco directement `Rejected` à l'arrivée si
  son type est dans `_ignoredRecommendationTypes`* : casse la
  capacité utilisateur à changer d'avis depuis l'historique (rien
  d'utile à examiner si tout est déjà Rejected). On préfère garder
  la nouvelle `Pending` et bloquer juste l'auto-popup.
- *Supersession dans `EventDetector` au lieu du journal (ne pas
  réémettre l'event si type récent suppressé)* : viole §9 (le
  détecteur doit refléter ce que les capteurs voient, pas
  l'historique des décisions). On suppress à l'étage présentation,
  pas à l'étage mesure.

---

### 45. Verrouillage du scope MVP par principe de complétude fonctionnelle

**Contexte** : projet en finition Étape 10 (sub-étape 10b-perf). Audit
interne identifie plusieurs chantiers ouverts hétérogènes (chalara
dormant, EddyTower visuel sans réalité, WeatherStation sans Reader,
3 onglets Niveau B vides, faune en backlog, capital absent, biodiv
scalaire). Risque réel de scope creep ou son inverse (livrer un MVP au
goût d'inachevé). Audience portfolio cible : recruteurs tech + jury
M1, qui ont des exigences cohérentes mais distinctes.

**Décision** : verrouiller le scope MVP par principe de complétude
fonctionnelle. Audience prioritaire = recruteurs tech (Unity/WebGL/
archi logicielle) et jury M1 (rigueur scientifique). Budget = 15-20h/
semaine sur 3 mois max, cible 150 h. Principe directeur : « rien en
scène ou en code ne donne le goût d'inachevé ». Corollaire :
« compléter ou supprimer (jamais laisser en l'état) ». Détaillé dans
`CLAUDE.md` §17 et §18.

**Raison** : un portfolio honnête et un jury M1 exigent un MVP cohérent
end-to-end, pas une accumulation de features partielles. La complétude
fonctionnelle est ce qui crée l'effet « production-ready » recherché
par les recruteurs et défendable scientifiquement par un jury.

**Conséquence opérationnelle** : ouvre 5 chantiers fermants (ADRs #46
à #54) déroulés sur les étapes E1-E7 de la nouvelle `ROADMAP.md`.
Suppression de la stratégie de coupe pré-décidée (cf ADR #56).

**Alternative écartée** : continuer en mode « feature après feature
avec backlog grossissant » — résultat : MVP au goût d'inachevé,
défendable ni en recrutement ni en soutenance.

---

### 46. Purge totale du code chalara

**Contexte** : la détection chalara a été désactivée en sub-étape 10b
polish capteur (le piège photo IR ne détecte pas un champignon,
sémantiquement faux). Les classes `HedgeChalaraEvent` et
`PlantHedgesRecommendation` ont été conservées dormantes en attente
d'un capteur adapté (cf ancien BACKLOG #16). À l'audit de recadrage :
avoir une seule maladie isolée (chalara, sur frêne uniquement) dans
un modèle sans autre pathogène (rouille blé, septoriose, sclérotinia
colza, processionnaire chêne) donne l'impression d'un modèle santé
végétale ébauché puis abandonné.

**Décision** : suppression totale du code lié à chalara. Pas de
réintroduction en MVP.

Implications mécaniques :

- Suppression de `Assets/_Project/02_Sensors/Events/HedgeChalaraEvent.cs`.
- Suppression de la branche pénalité chalara dans
  `HedgerowHealthIndicator.Compute()` + constante `ChalaraPenalty`.
- Suppression de la branche `case HedgeChalaraEvent` dans
  `RecommendationProvenance.SensorDisplayFor()`.
- 6 tests EditMode utilisant `HedgeChalaraEvent` → réécriture en
  remplaçant les références `hedge-chalara#NN` par `drought-prolonged#NN`
  et `PlantHedgesRecommendation` par `IrrigationAdviceRecommendation`
  comme fixture (préserve la couverture sur supersession et dedup).
- `docs/BACKLOG.md` : items #14 et #16 supprimés, remplacés par item
  « Cadre santé végétale complet » conditionnel à un modèle de
  phénologie cultures.

**Raison** : conforme au principe directeur §17 (CLAUDE.md). Soit on
remet un écosystème santé végétale d'un coup (pathologies + ravageurs
avec capteurs adaptés), soit rien. Le compromis « chalara seul
dormant » donne le goût d'inachevé que le MVP refuse explicitement.

**Conséquence opérationnelle** : chantier E1 de la nouvelle
`ROADMAP.md`. Le stash `stash@{0}` contient des patches cleanup
chalara partiels récupérables via `git stash show -p stash@{0}`.
Estimation 2-4 h (incluant réécriture tests).

**Alternative écartée** : réintroduire chalara avec un capteur adapté
(NDVI drone, enquête terrain) sans le reste de l'écosystème santé
végétale — ouvre à nouveau le problème de la maladie isolée.

---

### 47. Refactor unifié des actions manuelles via le journal

**Contexte** : à la sub-étape 10a, 3 boutons « interventions
ponctuelles » (PlantHedges, Irrigation, ReduceInputs) ont été câblés
via `SimulationRunner.ApplyManualXxx()` qui appliquent l'effet
directement sur le modèle réel, sans passer par le `DecisionJournal`.
Asymétrie discutable : les recos auto traversent journal + verdict +
supersession, les actions manuelles bypass complètement. Friction
audit recadrage : traçabilité du run réel incomplète, le futur
`SessionReporter` (jamais construit — BACKLOG #4) ne verrait pas les
actions manuelles.

**Décision** : toutes les actions manuelles passent par le
`DecisionJournal` sous forme de `IRecommendation` « manual »
auto-acceptée. Plus de bypass direct du modèle.

Implications mécaniques :

- `SimulationRunner.ApplyManualXxx()` → créent une `IRecommendation`
  avec `DefaultVerdict = AutoAccepted` et l'ajoutent au journal via
  `DecisionJournal.Append()`.
- `AutoActionPipeline.Apply()` reste seul à modifier le modèle (pas
  de bypass).
- Convention `Id` : `"manual-plant-hedges#<day>"`,
  `"manual-irrigation#<day>"`, `"manual-reduce-inputs#<day>"`.
- Convention `TriggeredByEventId = null`. Adapter
  `RecommendationProvenance.Format()` : fallback « Action déclenchée
  par l'utilisateur le jour X » si `TriggeredByEventId == null`.
- Supersession des actions manuelles répétées : **cumulables**. Comme
  l'action manuelle est `AutoAccepted` dès création (pas `Pending`),
  elle ne déclenche pas la supersession des autres entrées du même
  type. `PlantHedges` +30 m/ha puis +30 m/ha → +60 m/ha total,
  2 entrées journal distinctes.
- `PlantHedgesRecommendation` reste utile (côté manuel uniquement —
  n'est plus émise par `RecommendationEngine.TryProduceFor` depuis
  10b).

**Raison** : discipline architecturale propre, traçabilité totale des
décisions joueur, supersession applicable, plus défendable pour
jury M1. Aligne la sémantique « auto » et « manuel » sur un même
canal.

**Conséquence opérationnelle** : chantier E1 de la nouvelle
`ROADMAP.md`. Estimation 3-4 h (refactor + tests).

**Alternative écartée** : garder le bypass actuel — viole le principe
de traçabilité unique et complique le futur `SessionReporter`.

---

### 48. Modèle carbone sol 1-pool + EddyTower bout-en-bout

**Contexte** : le sprite EddyTower (tour de covariance) est présent en
scène depuis l'Étape 6c mais sans variable d'état correspondante.
Violation pratique du principe primauté du capteur (CLAUDE.md §9).
L'item BACKLOG #13 (variable d'état carbone sol) attendait son tour.
À l'audit recadrage : soit on retire le sprite (perte d'un argument
scientifique majeur), soit on le branche.

**Décision** : implémenter le modèle carbone sol 1-pool dans le MVP.
Le sprite EddyTower devient un capteur fonctionnel bout-en-bout
(mesure → indicateur affiché, sans événement ni reco — conforme §17
principe directeur « OU indicateur affiché »).

Implications mécaniques :

- Nouvelle variable d'état `SoilCarbonStock` (tC/ha) dans
  `EcosystemModel`, default 50.
- Nouvelle règle `SoilCarbonDynamicsRule` (Couche 01) : modèle 1-pool
  `dC/dt = inputs − k·C`, `k = 1/40 an⁻¹` (calibration cf
  `CALIBRATION.md`).
- 2 nouveaux leviers dans `ScenarioContext` :
  `CoverCropsCoveragePercent` (0-100 %) et
  `ResidueRestitutionPercent` (0-100 %), avec sliders dans scenario
  panel.
- Nouveau `EddyTowerSensorReader` (Couche 02) : mesure flux net
  journalier CO2 avec bruit gaussien. Sous-flux RNG
  `"eddy-tower"`.
- Nouveau `SoilCarbonIndicator` (Couche 04) + `RC_SoilCarbonStock`
  (Data/RuntimeContainers).
- Affichage dans onglet Climat & Ressources (cf ADR #54).
- Panneau d'inspection EddyTower (cf ADR #53) : graphe flux journalier
  + stock cumulé.
- 4-5 tests EditMode.

**Raison** : brancher EddyTower renforce massivement la défensibilité
scientifique (sujet « 4 pour 1000 » INRAE, marchés volontaires CO2,
Label Bas-Carbone). Un sprite capteur sans réalité dans le modèle est
une violation visible du principe primauté du capteur, anti-portfolio.

**Conséquence opérationnelle** : chantier E3 de la nouvelle
`ROADMAP.md`. Sources : Solagro Afterres 2050, INRAE 4 pour 1000,
Efese services écosystémiques, BDAT. Estimation 10-14 h (incluant
panneau inspection EddyTower).

**Alternative écartée** : retirer le sprite EddyTower — résout le
problème de cohérence visuelle/code mais perd un argument
scientifique majeur du DT.

---

### 49. Faune visible — 4 espèces en pool avec animations frame-swap

**Contexte** : sans faune visible, l'indice `RC_BiodiversityComposite`
reste un chiffre abstrait. La disparition des espèces quand la biodiv
chute est le signal pédagogique central du sujet bocage. Items
BACKLOG #1 + #2 reportés depuis l'Étape 9. Ébauches de sprites déjà
disponibles dans `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/`
(4 espèces × 3-4 frames partiellement présents).

**Décision** : implémenter le pool de 4 espèces visibles (héron,
chouette, buse, hirondelle) avec animations frame-swap, courbes de
réponse sur la biodiv.

Implications mécaniques :

- `FaunaSpeciesDefinition.cs` : ScriptableObject par espèce avec
  sprites, seuil d'apparition, position de spawn, pattern d'animation.
- `FaunaPool.cs` (Couche 05) : object pooling sans Instantiate runtime
  (CLAUDE.md §6 conforme).
- `FaunaIdleMotion.cs` (Couche 05) : animation frame-swap simple
  (cycle de 3-4 frames).
- `FaunaPoolBinding.cs` (Couche 05) : observe
  `RC_BiodiversityComposite` + `RC_FaunaFactor*` (cf ADR #51) →
  spawn/despawn espèces selon courbes de réponse.
- Crunch DXT5 conditionnel sur les nouveaux sprites (cf décision
  conditionnelle dans `docs/ROADMAP.md` chantier E7).
- Pas de modulation `_HealthT` sur faune (item BACKLOG #3 supprimé
  définitivement, hors MVP).

**Raison** : la faune visible est l'élément qui transforme un
dashboard d'indicateurs en un digital twin vivant. Sans elle, la
chaîne pédagogique « intrants ↑ → biodiv ↓ → faune disparaît » reste
abstraite. Sprites ébauchés sans intégration = goût d'inachevé
explicitement refusé par §17.

**Conséquence opérationnelle** : chantier E4 de la nouvelle
`ROADMAP.md`. Sources : ZNIEFF Perche, ONF, PNR du Perche pour
bestiaire et seuils. Estimation 10-13 h (sprites déjà ébauchés,
corrections finales à charge utilisateur).

**Alternative écartée** : reporter la faune en post-MVP — viole le
principe directeur §17.

---

### 50. Capital d'investissement + horizon de rentabilité

**Contexte** : `IntegratedProfitabilityIndicator` agrège revenus −
coûts opérationnels + aides, sans notion de capital amortissable.
L'action `PlantHedges` (manuel via ADR #47) est sans coût upfront
représenté → arbitrage agriculteur faussé vers acceptation
systématique. Item BACKLOG #9 attendait son tour. Pour un jury M1,
c'est la critique facile : « votre modèle économique ignore le
capital, c'est inutilisable en conseil agricole ».

**Décision** : modéliser le capital d'investissement (sur PlantHedges
uniquement, seule action avec coût upfront réel) et calculer
l'horizon de rentabilité via shadow vs real.

Implications mécaniques :

- Champ `InvestmentCost` (€/ha) sur `IRecommendation` (calculé pour
  `ManualPlantHedgesRecommendation` : densité plantée × prix au m
  linéaire, source Réseau Haies 3-10 €/m).
- Texte « Coût upfront estimé : X €/ha » affiché dans popup décision
  (manuel).
- Cumul `TotalInvestment` dans `DecisionJournal` (somme des
  `InvestmentCost` des entrées appliquées).
- Nouveau `InvestmentHorizonIndicator` (Couche 04) : calcul des
  années pour récupérer l'investissement, basé sur divergence
  rentabilité réel vs shadow.
- Affichage : ligne « Horizon rentabilité : X ans » dans popup
  décision et onglet Économie. « Non encore atteint » si pas atteint
  dans la simulation.
- Pour Irrigation et ReduceInputs manuels : `InvestmentCost = 0`
  (action ponctuelle, coût intégré dans `InputCost`).

**Raison** : la thèse centrale du DT est « convergence honnête éco/
écolo ». Sans capital, planter est gratuit, donc trivial à accepter,
et la thèse est faussée. L'horizon rentabilité est l'argument décisif
d'un agriculteur réel — standard du métier (Chambre d'agriculture,
référentiel MAEC).

**Conséquence opérationnelle** : chantier E5 (groupé avec ADR #51).
Sources : Réseau Haies de France, MAEC référentiel coûts plantation,
FranceAgriMer prix blé/lait, Chambre d'agriculture. Estimation 6-8 h.

**Alternative écartée** : reporter en post-MVP — perd la critique
anticipable du jury M1.

---

### 51. Biodiv enrichie — exposition de 3 facteurs (refonte minimale)

**Contexte** : `BiodiversityCompositeIndicator` agrège 50 % fauna +
30 % hedge + 20 % water inverse, pondérations auto-justifiées sans
citation précise. Item BACKLOG #15 (refonte biodiv) attendait son
tour. Compromis MVP : ajouter un 4ème facteur « Diversité paysage »
demanderait 2 nouveaux sliders scenario (`GrasslandPercent`,
`CropDiversityIndex`) → complexité ajoutée.

**Décision** : refonte limitée — pas de 4ème facteur dans le MVP.
Exposition individuelle des 3 facteurs actuels (habitat, eau,
intrants) via des `RC_*` distincts pour affichage onglet Biodiv.
Recalibration des pondérations. Ajout d'effets faibles sourcés depuis
météo journalière (canicule) et carbone sol.

Implications mécaniques :

- `FaunaDynamicsRule` (Couche 01) refondue : 3 facteurs (habitat, eau,
  intrants) calculés explicitement, exposés via `RC_FaunaFactorHabitat`,
  `RC_FaunaFactorWater`, `RC_FaunaFactorInputs`.
- Ajout d'un effet faible météo journalière (canicule) sur fauna :
  pénalité au-delà de seuil T° quotidien (sourcé Hallmann 2017).
- Ajout d'un effet faible carbone sol sur fauna : bonus si stock
  C > seuil (sols vivants = plus de macrofaune).
- Recalibration des pondérations du `BiodiversityCompositeIndicator`
  sur base littérature (Vigie-Nature, Hallmann 2017, MNHN 2024).
- 3 lignes affichables dans onglet Biodiv (cf ADR #54).

**Raison** : compromis raisonnable. 3 lignes affichables,
scientifiquement défendable, sans complexité ajoutée des nouveaux
sliders scenario qui auraient demandé un retravail UI scenario panel.

**Conséquence opérationnelle** : chantier E5 (groupé avec ADR #50).
Sources : INRAE Vigie-Nature, Constant et al. 1976 (Réseau Haies),
Hallmann et al. 2017 (Krefeld), MNHN 2024. Estimation 6-8 h. Partie
reportée en BACKLOG (4ème facteur Diversité paysage).

**Alternative écartée** : refonte complète avec 4ème facteur +
2 sliders — coût plus élevé sans gain MVP critique.

---

### 52. Saisonnalité + WeatherStation chaîne complète

**Contexte** : `WeatherUpdateRule` tire chaque jour autour de moyennes
annuelles fixes (12 °C, 2 mm/jour), sans cycle saisonnier. Le jour 1
et le jour 180 ont la même distribution météo. Item BACKLOG #12
attendait son tour. Manque scientifique le plus visible aux yeux
d'un agroécologue. Sprite WeatherStation présent depuis 6c sans
Reader formel. Audit recadrage : double problème (modèle + chaîne
capteur incomplète) résoluble d'un coup.

**Décision** : implémenter Piste J intégrale — saisonnalité avec
données mensuelles Météo-France (station Mortagne-au-Perche 61,
normales 1991-2020), modèle stochastique Niveau 3 (chaîne de Markov
ON/OFF pour pluie + log-normale intensité), WeatherStation comme
capteur de mesure pure bout-en-bout.

Implications mécaniques :

- `SeasonalWeatherDataAsset.cs` (Couche 01) : ScriptableObject avec
  12 valeurs T° + 12 valeurs précip + paramètres Markov mensuels
  (p_wet, mu, sigma).
- Refonte `WeatherUpdateRule` : lit le mois courant + anomalies
  scenario + tire Bernoulli(p_wet[mois]) puis LogNormal(mu[mois],
  sigma[mois]) si pluvieux + bruit gaussien T° (σ = 2 °C). Sous-flux
  RNG `"markov-rain"` et `"weather-noise"`.
- Widget « Mois de démarrage » (combo Jan-Déc) dans section
  « Conditions initiales ».
- `WeatherStationReader` (Couche 02) : mesure pure T° + précip avec
  bruit gaussien. Pas d'événement, pas de reco — lecture pure
  (option a actée).
- Cascade saisonnière gratuite : `WaterTableDynamicsRule`,
  `HedgerowGrowthRule`, `FaunaDynamicsRule` deviennent saisonnières
  via leurs inputs (water table notamment).
- Extension `CropYieldDynamicsRule` + `InputCostDynamicsRule` à la
  météo journalière (option a) : ajout d'un terme dépendant de la
  météo réelle (canicule WeatherStation → effet direct économique).
- Panneau « Normales climatiques mois courant + suivant » intégré au
  panneau inspection WeatherStation (cf ADR #53).
- Crises saisonnières (canicule, inondation) et effets visuels
  saisonniers (ciel, prairie) en BACKLOG hors MVP.

**Raison** : sans saisonnalité, le DT est défendable en démonstration
technique mais inattaquable scientifiquement par un agroécologue.
WeatherStation sans Reader formel viole le principe primauté du
capteur. Résolution conjointe = haute valeur portfolio.

**Conséquence opérationnelle** : chantier E2 de la nouvelle
`ROADMAP.md`. Sources : Météo-France normales 1991-2020 station
Mortagne-au-Perche (61), INRAE échelle BBCH, ARVALIS Eure-et-Loir.
Estimation 16-22 h (16 h base + 3 h extension CropYield/InputCost +
6-10 h niveau 3 Markov).

**Alternative écartée** : saisonnalité moyennes annuelles + bruit
seul (sans Markov) — moins défendable scientifiquement, le gain de
complexité du Markov est modeste pour un bénéfice élevé en jury.

---

### 53. Panneau d'inspection des capteurs cliquables

**Contexte** : les 5 capteurs sont visibles en scène mais ne révèlent
leurs mesures que via les indicateurs Hero ou Niveau B agrégés. Aucun
moyen d'inspecter directement un capteur, de voir sa série de mesures,
de comprendre l'incertitude (acoustique fragile à faible densité par
exemple).

**Décision** : les 5 capteurs deviennent cliquables. Un panneau
d'inspection s'ouvre au clic, avec un contenu spécifique par capteur
(graphes des mesures historiques vs références).

Contenu par capteur :

| Capteur | Contenu du panneau au clic |
|---|---|
| Piezometer | Graphe profondeur nappe 365 j + 2 seuils (3,5 m alerte drought, 5 m critique) + compteur « jours consécutifs > 3,5 m ». |
| WeatherStation | 2 graphes superposés : T° journalière vs normale mensuelle, précip journalière vs normale mensuelle. Affichage normales mois courant et suivant. |
| AcousticSensor | Graphe abondance mesurée (bruitée) vs vraie abondance (modèle). Visualise l'incertitude — pédagogie acoustique fragile à faible densité. |
| CameraTrap | Idem AcousticSensor. Permet de comprendre la fusion via `FaunaSensorReader`. |
| EddyTower | Graphe flux journalier CO2 + stock C cumulé (cf ADR #48). |

Implications mécaniques :

- Détection clic sur sprite 2D : `Collider2D` + `IPointerClickHandler`
  via Unity EventSystem + `Physics2DRaycaster` sur la caméra.
- Stockage sliding window 365 j dans chaque `*SensorReader`
  (mutualisé via interface `ISensorHistory<T>`, partagé avec ADR #54
  onglets).
- Composant `SensorInspectorPanel.uxml` (UXML + USS) réutilisable, se
  reconfigure selon le capteur cliqué.
- Composant graphe custom en `VisualElement` avec
  `generateVisualContent` callback.
- Fermeture : clic dehors, touche Échap, bouton fermer.
- Nouveau binding `SensorInspectorPanelBinding` (Couche 05).

**Raison** : transforme les capteurs de « décor instrumenté » en
« interfaces d'inspection », aligné avec l'identité station
d'observation du DT. Permet à un visiteur portfolio de comprendre
l'incertitude de mesure en 2 clics, signal de maturité scientifique.

**Conséquence opérationnelle** : chantier E6 (groupé avec ADR #54).
Estimation 12-21 h (4-6 h système générique + 3-5 h graphe custom +
5-10 h contenus 5 capteurs).

**Alternative écartée** : afficher les séries de mesure dans un onglet
dédié — moins direct, casse la spatialité du DT.

---

### 54. 3 onglets Niveau B tous remplis

**Contexte** : les 3 panneaux Niveau B (Biodiversité, Climat &
Ressources, Économie) sont en place depuis l'Étape 6b mais largement
remplis de placeholders « à venir ». Friction visible : structure UI
riche, contenu pauvre.

**Décision** : les 3 onglets Niveau B sont tous remplis avec des
sous-indicateurs riches utilisant les variables existantes + nouvelles
(saisonnalité, carbone sol, faune visible, capital, biodiv 3 facteurs).

Contenu détaillé par onglet :

**Biodiversité** :

| Ligne | Variable source |
|---|---|
| Indice composite | `BiodiversityCompositeIndicator` |
| Composante habitat (haies) | `RC_FaunaFactorHabitat` (nouveau via ADR #51) |
| Composante eau | `RC_FaunaFactorWater` (nouveau via ADR #51) |
| Composante intrants | `RC_FaunaFactorInputs` (nouveau via ADR #51) |
| Comptage espèces visibles | dérivé de `FaunaPool` (nouveau via ADR #49) |

**Climat & Ressources** :

| Ligne | Variable source |
|---|---|
| Profondeur nappe | `WaterTableDepth` (déjà) |
| T° moyenne 365 j glissants | `CurrentWeather` history (nouveau via ADR #52) |
| Précipitations cumulées 365 j glissants | `CurrentWeather` history (nouveau via ADR #52) |
| Stock carbone sol | `SoilCarbonStock` (nouveau via ADR #48) |
| Flux net CO2 | `EddyTowerSensorReader` history (nouveau via ADR #48) |

**Économie** :

| Ligne | Variable source |
|---|---|
| Rendement cultures | `CropYield` (déjà) |
| Coût intrants | `InputCost` (déjà) |
| Coût entretien haies | `MaintenanceCost` (déjà) |
| Paiement PSE | calculé (déjà) |
| Paiement PAC (DPB + redistributif + écorégime + bonus haies) | constantes (déjà) |
| Investissement cumulé | `journal.TotalInvestment` (nouveau via ADR #50) |
| Horizon rentabilité | `InvestmentHorizonIndicator` (nouveau via ADR #50) |

Implications mécaniques :

- Nouveaux bindings : `OngletBiodivBinding`, `OngletClimatBinding`,
  `OngletEconomieBinding` (Couche 05).
- Sliding windows 365 j pour `CurrentWeather` history et `EddyTower`
  flux history mutualisées avec celles d'ADR #53.
- USS / UXML existants des onglets à enrichir.

**Raison** : avec toutes les pistes activées (E2-E5), on a précisément
créé les variables qui remplissent ces onglets. Les retirer serait
gâcher le bénéfice des décisions précédentes. Aligné avec principe
directeur §17 « tout onglet présent doit afficher de l'info utile ».

**Conséquence opérationnelle** : chantier E6 (groupé avec ADR #53).
Estimation 10-12 h.

**Alternative écartée** : remplir partiellement avec les variables
existantes seulement — résultat : 3 onglets affichant 2-3 lignes
chacun, goût d'inachevé refusé par §17.

---

### 55. Pattern rationale uniforme (action concrète + Effet modélisé)

**Contexte** : 3 propositions de wording précédentes pour les recos
avaient été rejetées car elles évoquaient des effets non modélisés
(auxiliaires, brise-vent secondaire, résilience générale). Le
`RecommendationPopupBinding` actuel affiche des rationales
hétérogènes selon l'origine de la reco.

**Décision** : adopter un pattern uniforme de rédaction des rationales
pour toutes les recommandations (manuelles ET auto). Format : Title
court (verbe + objet) + Rationale = phrase d'action concrète + ligne
`Effet modélisé : ...` chiffrée sur les variables effectivement
touchées. Pas d'envolée, pas de chimères non modélisées.

Wordings exacts pour actions manuelles :

| Reco | Title | Rationale |
|---|---|---|
| `manual-plant-hedges` | Planter des linéaires de haies | Plantation d'essences sur bordures de parcelles. Effet modélisé : +X m/ha de densité de haies, +Y €/ha/an de coût d'entretien proportionnel. |
| `manual-irrigation` | Irrigation ponctuelle | Apport d'eau ciblé sur 30 jours. Effet modélisé : remontée temporaire de la nappe phréatique de X m (plancher 0,5 m). |
| `manual-reduce-inputs` | Baisser l'intensité d'intrants | Réduction des intrants chimiques sur 30 jours. Effet modélisé : +Y de population faune, −Z €/ha de coût d'intrants. |

X, Y, Z = valeurs paramétrées par le slider de magnitude au moment du
clic.

Uniformisation des recos auto (option α actée) : appliquer le même
pattern aux 2 recos auto existantes, en ajoutant une ligne
`Déclenché par : <événement>` en plus :

- `IrrigationAdviceRecommendation` (auto) : Title « Irrigation ciblée
  + couvert anti-évaporation » ; Rationale « Apport d'eau ciblé +
  couverts sur 30 jours. Effet modélisé : ... Déclenché par :
  Sécheresse prolongée détectée par le piézomètre. »
- `ReduceInputsRecommendation` (auto) : Title « Baisser l'intensité
  d'intrants » ; Rationale « Réduction des intrants chimiques sur
  30 jours. Effet modélisé : ... Déclenché par : Anomalie acoustique
  faune détectée par le capteur acoustique. »

**Raison** : la ligne `Effet modélisé : ...` indique explicitement
les limites du modèle — discipline qu'on revendique partout. Format
uniforme = lecture immédiate par le visiteur, et garde-fou contre
les chimères non modélisées.

**Conséquence opérationnelle** : chantier E1 (couplé refactor actions
manuelles ADR #47). Réécriture libellés. Estimation incluse dans E1.

**Alternative écartée** : rationales libres au gré des recos — perd
l'uniformité et risque la mention d'effets non modélisés.

---

### 56. Suppression de la stratégie de coupe pré-décidée

**Contexte** : section §17 historique de `CLAUDE.md` listait un ordre
de coupe (décision moyenne → suppression healthT → réduction tests →
réduction sprites → ne pas couper architecture). Audit recadrage :
le scope est verrouillé par cette session (cf ADR #45), le slack
budget est confortable (~30-65 h sur cible 150 h), les dépassements
historiques étaient liés à des pivots de scope (maintenant interdits
par discipline §18 règle 2), pas à de mauvaises estimations.

**Décision** : la section §17 « stratégie de coupe finale » de
`CLAUDE.md` est supprimée. Pas de stratégie de coupe pré-décidée.
Si on dépasse 150 h, l'utilisateur arbitre au cas par cas en
cohérence avec le principe directeur.

**Raison** : cohérent avec la règle « compléter ou supprimer » (§18
règle 8) — on choisit de ne pas avoir cette mécanique plutôt que d'en
avoir une à moitié. Avoir une stratégie de coupe documentée alors
qu'on ne compte pas l'utiliser invite à l'auto-justification de
raccourcis.

**Conséquence opérationnelle** : §17 supprimé dans `CLAUDE.md`,
remplacé par §17 Scope MVP + §18 Discipline. §18 En cas de doute
renuméroté en §19.

**Alternative écartée** : conserver une stratégie de coupe « au cas
où » — contredit le scope verrouillé et le principe directeur.

---

### 57. Tous les capteurs rendus comme « branché » — concept « en attente » reporté

**Contexte** : `SensorPlacement_Default.asset` distinguait historiquement
des capteurs `Online` et `Deferred` (visuellement : dot vert vs dot
ocre dans la liste « Capteurs déployés », plus une légende au pied de
la liste). État au 2026-06-02 (livraison E6) : les 5 capteurs ont tous
une chaîne complète bout-en-bout — `PiezometerReader`,
`WeatherStationReader`, `EddyTowerSensorReader`, et les deux canaux
`AcousticSensorReader`/`CameraTrapSensorReader` exposés par
`FaunaSensorReader` ont chacun un historique 365 j et alimentent le
panneau d'inspection (ADR #53). Aucun capteur n'est plus « en
attente » au sens technique.

Mais corriger le SO (passer les 3 capteurs encore marqués `Deferred`
à `Online`) a buté sur un cache Unity tenace : le fichier disque
corrigé n'était pas relu, et même après reimport explicite la liste
UI continuait à afficher gris. Forcer la valeur via l'Inspector Unity
fonctionnait sur l'asset mais pas en runtime — déconnexion non
diagnostiquée à temps raisonnable.

**Décision** :

- `SensorListBinding.BuildRow` ignore désormais `meta.OnlineStatus`
  et applique inconditionnellement la classe `.sensor-status-dot--online`.
- La légende online/deferred au pied de `Dashboard.uxml` (bloc
  `.sensor-list-legend`) est supprimée — un seul état visuel ne mérite
  pas une légende.
- Le champ `OnlineStatus` reste présent dans `SensorPlacementDefinition`
  et `SensorMetadataTag` pour ne pas perdre la donnée — quand un
  backlog item « capteur en panne / maintenance » réactivera le
  concept avec un VRAI cas d'usage scénaristique, le code y revient
  en retirant la ligne hardcodée et en restaurant la légende.

**Raison** : aligné avec le principe directeur §17 « tout élément
présent doit avoir un effet observable et un intérêt narratif
compréhensible ». Un dot ocre sans cas d'usage concret (pas de
scénario panne, pas de maintenance simulée, pas d'événement « capteur
défaillant ») est de l'info parasite — l'utilisateur portfolio voit
3 dots gris et se demande légitimement « qu'est-ce qui ne marche pas
chez moi ». Réponse : rien. Donc on retire le distinguo plutôt que
d'expliquer un faux problème.

**Conséquence opérationnelle** : aucune sur la roadmap E1-E7. Le
ré-introduction du concept est conditionnée à un futur item backlog
qui mettrait en scène un capteur intentionnellement offline (panne,
maintenance, batterie morte d'un sensor solar-powered, etc.) — ce
qui justifierait pédagogiquement la distinction visuelle.

**Alternative écartée** : continuer à débugger le cache Unity et
maintenir le distinguo. Diagnostic coûteux (déjà brûlé ~30 min sans
identifier la cause racine), zéro gain narratif tant que le concept
reste théorique.

---

### 58. Shadow run = contrefactuel à baseline gelée (frozen-baseline)

**Contexte** : à l'ouverture du chantier E8 (refonte du delta tech), la
chaîne shadow telle que documentée par les ADRs #9, #24 et #43 reposait
sur deux idées qui ne tenaient plus à l'implémentation. (1) #9 et #24
décrivaient un shadow run « mêmes seeds, mêmes inputs » porté par une
interface `ISimulationRun` à deux instances et un flag `applyTechActions`.
(2) #43 supposait que le `ScenarioContext` était partagé par référence
entre run réel et shadow, ce qui interdisait à `ReduceInputs` de toucher
`InputIntensityFactor` (la shadow aurait subi la même baisse, annulant
le KPI). Aucune de ces deux constructions n'a survécu : `ISimulationRun`
et `applyTechActions` n'ont jamais été écrits, et le partage total du
scénario rendait impossible un changement de pratique mesurable.

**Décision** : le shadow run est un contrefactuel à baseline gelée. Il
partage avec le run réel les paramètres exogènes (climat, MAEC, PSE)
**par référence**, mais **gèle à leur valeur de lancement** les quatre
leviers de décision agriculteur (`HedgeRemovalRate`,
`InputIntensityFactor`, `CoverCropsCoveragePercent`,
`ResidueRestitutionPercent`) via `ScenarioContext.CreateFrozenShadowFrom`.
Le shadow possède son propre `EcosystemModel` et avance par
`TickWithoutAdvancingScenario` (il ne fait pas progresser le scénario,
qui reste piloté par le run réel). Le KPI de valeur tech mesure
exactement l'écart réel-vs-agriculteur-gelé : tout ce que l'utilisateur
change après le lancement diverge du jumeau figé.

**Raison** :
- Un shadow « mêmes inputs » partagé par référence ne peut pas servir de
  contrefactuel dès qu'une décision modifie le scénario : il bouge avec
  le run réel et le delta s'annule. Geler les seuls leviers agriculteur,
  tout en partageant le climat et les cadres de paiement, isole
  proprement la contribution des décisions de gestion — c'est la
  sémantique « jumeau sans décisions tech » que la thèse du DT prétend
  mesurer (cf #43, tension désormais résolue).
- Partager les exogènes par référence garantit qu'aucune divergence ne
  provient du climat ou des barèmes : la non-divergence due au scénario
  exogène est structurelle, pas à recalibrer.
- Débloque le changement de pratique : `ReduceInputs` peut redevenir un
  vrai curseur sur `InputIntensityFactor` (transition §15) sans casser
  le KPI, puisque la baseline gelée ne suit pas.

**Conséquence opérationnelle** : remplace le cadrage « mêmes inputs » des
ADRs #9 et #24 et renverse la prémisse du scénario partagé de l'ADR #43.
`ISimulationRun` / `applyTechActions` sont actés comme jamais construits
(fantômes). Preuves dans le code : `ScenarioContext.CreateFrozenShadowFrom`,
`ShadowSimulationRunner` (second `SimulationEngine` concret +
`TickWithoutAdvancingScenario`).

**Alternative écartée** : cloner intégralement le `ScenarioContext` pour
donner au shadow un scénario indépendant — perd le partage des exogènes
(le climat divergerait), réintroduit l'invariant d'unicité du scénario
discuté en #43, et brouille la sémantique du delta.

---

### 59. « Apport de la techno » = valeur NET cumulée (gain brut intégré moins investissement des actions), payback = jour où le NET franchit 0

**Contexte** : à la refonte E8, le Hero KPI « delta tech » devait
quantifier honnêtement l'apport de l'instrumentation et des décisions.
Le cadrage implicite hérité de l'ADR #40 (« agrégat calculable sur
(real − shadow) ») laissait penser à un écart instantané de rentabilité.
Or l'effet d'une action ponctuelle sur l'écart instantané fait un pic au
moment de l'action puis décroît vers 0 quand le système se rééquilibre :
un KPI instantané afficherait alors un apport qui « s'évapore », ce qui
est faux du point de vue de la valeur réellement créée.

**Décision** : le KPI intègre depuis le jour 0 l'écart journalier de
rentabilité intégrée entre le run réel et le shadow frozen-baseline
(grandeur **brute**, cumulée), puis **soustrait le capital upfront
cumulé des actions** (coûts des capteurs exclus) pour afficher la valeur
**NET** en €/ha. L'horizon de rentabilité (« payback ») latche le
**premier jour où le NET atteint l'équilibre** (NET ≥ 0).

**Raison** :
- Intégrer capitalise la valeur réellement créée : un pic transitoire qui
  retombe à 0 a quand même produit de la valeur sur sa durée, et
  l'intégrale la conserve. On juge une stratégie sur son horizon vrai,
  pas sur un instantané trompeur.
- Soustraire l'investissement des actions donne un NET honnête : un gain
  brut élevé obtenu au prix d'un capital lourd n'est pas le même résultat
  qu'un gain brut modeste gratuit. Le payback (jour où le NET franchit 0)
  est l'argument décisif côté agriculteur.
- Exclure les coûts capteurs : l'instrumentation est l'hypothèse du DT
  (le poste « observer »), pas une action de gestion comptabilisée dans
  l'arbitrage ; on mesure l'apport des décisions, capteurs supposés en
  place.
- Supersède le cadrage instantané suggéré par l'ADR #40.

**Conséquence opérationnelle** : preuves dans le code —
`CumulativeTechValueIndicator` (gain brut intégré),
`InvestmentHorizonIndicator` (latch du payback NET), `SimulationRunner`
(`net = gross − totalInvestment`).

**Alternative écartée** : afficher l'écart instantané de rentabilité —
spike puis décroissance vers 0, sous-estime massivement la valeur d'une
stratégie dont l'effet est transitoire mais réel, et rend le KPI
illisible dans le temps.

---

### 60. Réponse rendement concave (Mitscherlich) + coût intrants fixe/variable (70/30) ⇒ optimum de profit émergent I\* ≈ 0,81

**Contexte** : la réponse du rendement à l'intensité d'intrants était
linéaire, et le coût des intrants était traité comme entièrement
variable. Conséquence : le profit était monotone en intensité (plus
d'intrants = toujours plus ou toujours moins de profit selon les pentes),
sans optimum intérieur. Or les recommandations économiques de E9
(notamment « remonter les intrants vers l'optimum ») n'ont de sens que
s'il existe un point de profit maximal vers lequel orienter l'agriculteur.

**Décision** : remplacer la réponse linéaire rendement-vs-intensité par
une courbe concave à plateau (type Mitscherlich, **courbure 0,70**,
plateau au-delà de I = 1), et scinder le coût des intrants en **70 % fixe
structurel + 30 % variable** (`VariableCostShare = 0.30`). La combinaison
« rendement à rendements décroissants + part variable du coût » fait
émerger un **maximum de profit intérieur près de I ≈ 0,8** (optimum
calculé I\* ≈ 0,81), cible vers laquelle les recommandations économiques
orientent.

**Raison** :
- Une réponse concave est la forme agronomique correcte (loi des
  rendements décroissants : chaque unité d'intrant supplémentaire rapporte
  moins). Le plateau borne le gain au-delà de la dose de référence.
- Une part de coût fixe (structure, mécanisation, foncier) qui ne décroît
  pas avec l'intensité est ce qui crée l'optimum intérieur : sans elle, le
  profit resterait monotone. Le couple courbure/part variable est ce qui
  produit I\* ≈ 0,81.
- Donne un point d'ancrage chiffré et défendable aux contre-recommandations
  économiques de l'ADR #61 (« remonter vers I\* »).

**Conséquence opérationnelle** : sources et dérivation de la courbure
0,70, de `VariableCostShare = 0.30` et du calcul de I\* dans
`CALIBRATION.md` section E8-E9. La cible I\* est consommée par le moteur
de recommandations (ADR #61).

**Alternative écartée** : conserver la réponse linéaire + coût tout
variable — pas d'optimum intérieur, donc les recommandations économiques
« remonter/baisser les intrants vers la cible » n'auraient aucun point
de convergence à viser.

---

### 61. Système de recommandations E9 : 8 recos / 6 leviers, dispatch état-conscient, contre-recommandations économiques, surfaçage popup-vs-liste par classification d'outcome

**Contexte** : le moteur de recommandations comptait 3 recos (irrigation,
réduction d'intrants, plantation manuelle) sur un faible nombre de
leviers, toutes orientées « plus d'écologie ». Trois manques pour le
chantier E9 : (1) aucune recommandation économique de redressement quand
la rentabilité décroche, (2) aucun déclencheur sur le carbone sol bas
malgré le modèle 1-pool (ADR #48), (3) un surfaçage indifférencié — toute
reco interrompait par popup, sans distinguer un gain franc d'un
compromis chargé de valeurs.

**Décision** : passer de 3 à 8 recommandations sur 6 leviers (nouveaux :
`RaiseInputs`, `SowCoverCrops`, `RestoreResidue`, `ReduceHedgeRemoval`,
`IncreaseHedgeRemoval`). Le moteur opère un **dispatch état-conscient** :
il sélectionne le levier qui a une marge de manœuvre réelle dans l'état
courant (et reste silencieux si aucun, conforme §17), et émet des
**contre-recommandations économiques** (remonter les intrants vers
I\* — cf ADR #60 ; éclaircir des haies surdenses non subventionnées) sur
un nouvel **`LowProfitabilityEvent`** (seuil 50 €/ha — événement de seuil
d'indicateur, **pas** une lecture capteur), aux côtés d'un nouveau
**`SoilCarbonLowEvent`** (seuil 45 tC/ha). Les contre-recommandations
économiques sont **conditionnées à une biodiversité ≥ 0,30** (on ne
pousse pas à intensifier quand l'écosystème est déjà critique). Chaque
reco est classée par le signe de ses deltas projetés long terme
(profit / biodiversité) dans `RecommendationSurfacing.Kind` ∈ {`WinWin`,
`EconomicTradeoff`, `EcologicalTradeoff`, `LoseLose`}. Surfaçage :
- `WinWin` → **toujours** en popup.
- `EcologicalTradeoff` → popup **uniquement** si biodiversité critique
  (< 0,30).
- `EconomicTradeoff` → reste dans la **liste passive**, avec un badge
  « compromis » ; n'interrompt pas.
- `LoseLose` → non poussé.

**Raison** :
- Un moteur qui ne sait que recommander « plus d'écologie » n'est pas un
  outil d'aide à la décision honnête : un agriculteur dont la rentabilité
  décroche a besoin de leviers économiques. Les contre-recommandations,
  gatées sur biodiv ≥ 0,30, équilibrent la thèse sans trahir l'écologie.
- Le dispatch état-conscient (levier avec marge) évite de recommander une
  action sans effet (ex. réduire des intrants déjà bas) et justifie le
  silence quand aucun levier n'a de marge (§17).
- Classer par le signe des deltas projetés rend le surfaçage **dérivé du
  modèle**, pas d'un script : seul un gain franc (win-win) ou un arbitrage
  écologique en situation critique mérite d'interrompre ; tout compromis
  chargé de valeurs (économique) reste passif et signalé « compromis »,
  laissant l'arbitrage à l'utilisateur.
- `LowProfitabilityEvent` est explicitement un événement de **seuil
  d'indicateur** (rentabilité < 50 €/ha), pas une mesure capteur :
  cohérent avec le principe primauté du capteur (§9), il dérive d'un
  calcul du modèle tracé jusqu'à `IntegratedProfitability`.

**Conséquence opérationnelle** : preuves dans le code —
`RecommendationEngine`, `RecommendationSurfacing`, les 5 nouvelles recos,
`SoilCarbonLowEvent` / `LowProfitabilityEvent`. Table de surfaçage
(Kind × condition → popup/liste) dans `CALIBRATION.md`.

**Alternative écartée** : conserver 3 recos toutes écologiques et un
surfaçage popup uniforme — moteur déséquilibré (aucun redressement
économique), et popups intrusifs sur des compromis que l'utilisateur
devrait arbitrer lui-même dans la liste.

---

### 62. Décision dérivée du modèle : projection forward, objectif d'agriculteur, optimum émergent

**Contexte** : les outcomes affichés sous chaque recommandation (les
fourchettes profit / biodiversité pire-attendu-meilleur) étaient des
**coefficients figés** (ancien `OutcomeProjector`), indépendants de l'état
courant. Trois conséquences : (1) la projection pouvait mentir sur l'état
(sous stress climatique RCP4.5, une reco affichait un gain que le modèle
contredit), (2) l'optimum de profit était **chiffré en dur** (`I* ≈ 0,8`,
cf ADR #60), (3) la sélection du levier suivait une **priorité fixe** (cf
ADR #61). Pour un digital twin, les recommandations ET leurs outcomes
doivent être **dérivés du modèle couplé**, pas affirmés.

**Décision** : refondre la chaîne de décision pour qu'elle se calcule sur
le modèle.
- **`ModelOutcomeProjector`** (Couche 03) : pour un levier, simule en avant
  (vrai `SimulationEngine`, sur une copie indépendante de l'état) le run
  « avec levier » contre une baseline « sans », même graine et même météo,
  et prend le ΔKPI réel (profit, biodiversité). La bande pire/attendu/meilleur
  est le **spread sur 3 réalisations météo** (favorable / médiane /
  défavorable), pas un ×0,5 / ×1,25 arbitraire. Les indicateurs Couche 04
  sont injectés en délégués : la Couche 03 ne dépend pas de la 04.
- **`FarmerObjective`** : une fonction-objectif interne
  `U = w_eco · profit̂ + w_bio · Δbiodiv`, à **poids d'agriculteur**
  (économie dominante `w_eco = 0,80` ; biodiversité directe faible
  `w_bio = 0,20`, mais qui entre fortement par l'économie — le profit
  projeté embarque déjà l'effet brise-vent des haies, la fertilité du sol,
  les aides PSE/MAEC et la résilience du rendement). Poids internes (pas de
  nouveau curseur, §17), sourcés sur la littérature de décision agricole
  (Edwards-Jones 2006 ; Reimer et al. 2012).
- **Sélection par ΔU** : pour chaque événement, le moteur construit les
  leviers **faisables** (garde-fous de marge conservés, §17), projette
  chacun, et garde celui qui améliore le mieux `U`.
- **Optimum émergent** : le `0,8` en dur disparaît
  (`RaiseInputsRecommendation.ProfitOptimalIntensityFactor` supprimé). Une
  contre-recommandation économique ne se déclenche **que si la projection
  montre un gain de profit réel** — au-delà de l'optimum, remonter les
  intrants projette une perte et est écarté. L'optimum se recalcule donc
  tout seul si la calibration bouge.
- **Surfaçage dérivé du vrai** : `RecommendationSurfacing` classe à partir
  de l'`OutcomeDistribution` réelle (logique signe → Kind inchangée). Les
  bindings popup/liste **mémoïsent** la projection (forward sim = milliers
  de ticks, jamais sur un chemin par frame). Un événement décliné est
  **marqué considéré** (`DecisionJournal.MarkEventConsidered`) pour ne
  jamais être re-projeté.

**Raison** :
- C'est ce qui rend la thèse honnête ET rigoureuse : avec des poids
  d'agriculteur (économie d'abord), l'écologie n'est recommandée que là où
  l'instrumentation révèle qu'elle paie — la réponse **émerge du modèle
  couplé**, elle n'est imposée ni par les poids ni par des coefficients.
- L'optimum dérivé supprime une valeur magique (« précis et inattaquable,
  toute approximation assumée »). La calibration concave + coût 70/30 de
  l'ADR #60 (qui FAIT exister l'optimum) reste ; seule sa valeur n'est plus
  écrite en dur.
- Chaque projection sert une décision réelle (sélection, gating, surfaçage)
  — pas de mécanique décorative (§17).

**Conséquence opérationnelle** : preuves dans le code —
`ModelOutcomeProjector`, `FarmerObjective`, `RecommendationEngine`
(sélection par ΔU + gating économique), `RecommendationSurfacing`,
`DecisionJournal.MarkEventConsidered`, bindings `DecisionPopupBinding` /
`DecisionPanelBinding`. Poids `w_eco` / `w_bio` + échelle de normalisation
profit (150 €/ha) documentés dans `CALIBRATION.md`. 261 tests EditMode verts
(runner dotnet headless, Couches 01-04).

**Supersession** : remplace le projecteur à coefficients figés et le
dispatch à priorité fixe de l'ADR #61 (la table de surfaçage Kind × condition
→ popup/liste reste valable, mais les signes viennent désormais de la
projection réelle). L'ADR #60 reste valable pour la forme rendement/coût qui
crée l'optimum ; seul l'ancrage chiffré `I* ≈ 0,8` n'est plus consommé en
dur — l'optimum émerge.

**Alternative écartée** :
- Garder les coefficients figés (moins cher en calcul) — mais la projection
  ment sur l'état (cas RCP4.5), exactement le défaut qu'un digital twin doit
  éviter.
- Optimisation continue de la magnitude (chercher la dose optimale du levier)
  — sur-ingénierie : l'utilisateur choisit la magnitude au curseur, la
  projection à magnitude par défaut suffit à classer (§17).
