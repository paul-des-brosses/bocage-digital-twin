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

### 33. Workflow Git : utilisateur gère, Claude Code propose

**Contexte** : qui exécute les commandes Git.

**Décision** : l'utilisateur exécute toutes les commandes Git
(commit, push, branche, merge). Claude Code propose des messages
Conventional Commits aux moments opportuns.

**Raison** : l'utilisateur garde le contrôle de l'historique Git
(important en portfolio public), Claude Code reste cantonné à la
production technique.

**Alternative écartée** : Claude Code commit directement — perte de
contrôle, risques de bruit dans l'historique.

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

### 35. Pas d'audio, pas de mobile, pas de modal intrusif

**Contexte** : éléments à exclure explicitement du scope.

**Décision** : pas d'audio (cf #28), pas de support mobile (cf #17), pas
de modal intrusif (intro, tutoriel, dialogue bloquant).

**Raison** : focus, scope tenable, cohérence avec une station
d'observation silencieuse.

**Alternative écartée** : "on verra plus tard" — amène scope creep.
