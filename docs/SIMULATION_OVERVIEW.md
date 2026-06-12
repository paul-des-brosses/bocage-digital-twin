# Le modèle, en clair

*Vulgarisation du modèle de simulation — pour qui veut comprendre **comment ça marche** sans lire le code ni les équations. Lecture ~15 min. Chaque section ouvre une porte « → détail + sources » vers la spec technique si tu veux descendre d'un cran.*

[← le README](../README.md) · [▶ prendre la démo en main](GUIDE.md)

---

## 1. De quoi il s'agit

Un **digital twin** d'un site bocager fictif mais plausible du Perche normand, instrumenté de 5 capteurs. Tu y pilotes une exploitation : tu règles le climat et 6 leviers de conduite, et tu observes les conséquences se propager — eau, carbone du sol, azote, rendement, biodiversité, rentabilité.

La thèse n'est **pas postulée** : le modèle *teste* si écologie et économie peuvent converger, il ne le suppose pas. Selon le scénario, l'apport d'une conduite réfléchie peut être positif, neutre ou négatif — et le twin le dit.

> **Ce qu'il n'est pas** : un modèle scientifique validé. Les ordres de grandeur sont sourcés publiquement et défendables, mais aucun paramètre n'a été audité par un agronome ou un hydrologue du Perche pour ce site précis.

---

## 2. L'idée centrale : un réseau, pas une étoile

La faiblesse classique d'un modèle agricole jouet, c'est que le climat ne frappe que le rendement, pendant que nappe, carbone et biodiversité vivent leur vie. Résultat absurde : « 10 ans de sécheresse et tout va bien sauf l'onglet éco ».

Ici, tout passe par un **carrefour** : **θ, la réserve en eau du sol racinaire** (un « seau » qui se remplit de pluie et se vide par évapotranspiration, méthode FAO-56). Une sécheresse vide θ → le rendement chute → **moins de résidus rendus au sol** → moins de carbone → le sol retient **moins d'eau** → θ baisse encore. La sécheresse se propage *en cascade*, par cette boucle de rétroaction, au lieu de rester cantonnée à un coin.

→ *détail (bilan hydrique, ETP de Hargreaves, la cascade) : [`08_MODELE.md` §5.1-5.2](refonte/08_MODELE.md)*

---

## 3. Les 6 leviers — « optimiser, pas moraliser »

Tu agis sur : **azote**, **pesticides (IFT)**, **travail du sol**, **couverts d'interculture**, **gestion des haies**, **part de prairie**. Plus deux molettes climat et un mois de départ.

Le principe directeur : **chaque levier a un vrai inconvénient.** Mettre plus d'azote augmente le rendement *mais* coûte cher, lessive, et banalise la biodiversité. Convertir en prairie sécurise un revenu fourrager *mais* sacrifie la culture en bonne année. Du coup l'optimum n'est jamais à un extrême : il est **à l'intérieur**, et il *bouge* avec le climat. Le modèle ne pousse pas une morale (« plus vert = mieux ») ; il laisse l'optimum **émerger**.

→ *détail (chaque levier, son downside, l'optimum intérieur démontré) : [`08_MODELE.md` §5](refonte/08_MODELE.md) · [`11_VERIFICATION-MATHS.md`](refonte/11_VERIFICATION-MATHS.md)*

---

## 4. Ce que le modèle calcule

Six sous-modèles tournent à chaque tick (1 tick = 1 jour). En version courte :

- **Eau** — un seau FAO-56 : pluie en entrée, évapotranspiration en sortie, drainage au-delà de la capacité. C'est le carrefour du §2. La nappe suit, avec inertie.
- **Azote** — un **bilan explicite** (kgN/ha) : fertilisation + minéralisation de la matière organique + dépôt atmosphérique + fixation des couverts − prélèvement de la culture − lessivage − pertes gazeuses. L'azote disponible plafonne le rendement.
- **Carbone du sol** — un modèle **ICBM à 2 compartiments** (jeune/vieux), dont la décomposition s'accélère au chaud et humide. Les apports viennent des résidus, des couverts, des haies et de la prairie.
- **Rendement** — un potentiel atteignable rogné par 4 stress multiplicatifs (eau, azote, chaleur, adventices), intégrés sur la saison de croissance puis figés à la récolte. La réponse à l'azote est **saturante** (Mitscherlich) : doubler la dose au-delà de l'optimum ne gagne presque rien. Calibré sur Agreste/Arvalis : ~5,5 t/ha représentatif, avec une **variabilité interannuelle réaliste** (~13 %) selon la météo de l'année.
- **Biodiversité** — un indice composite de **3 facteurs** (habitat = haies + prairie ; eau = profondeur de nappe ; intrants = pression chimique inversée), plus deux modulateurs faibles (pénalité canicule, bonus sol vivant). C'est lui qui décide quels oiseaux apparaissent à l'écran.
- **Économie** — la marge = revenus (culture + fourrage) − coûts (azote, phyto, travail, entretien, charges fixes) + **paiements de services écosystémiques** (PAC, PSE haies, MAEC, crédit carbone). L'écologie « paie » donc en euros traçables.

→ *détail de chaque équation, paramètre et source : [`08_MODELE.md`](refonte/08_MODELE.md) (le tableau de paramètres sourcé est en §8)*

---

## 5. Les capteurs et l'incertitude

Le DT comporte **5 capteurs**, chacun bout-en-bout (mesure → indicateur affiché *ou* événement → recommandation) :

| Capteur | Mesure |
|---|---|
| Station météo | température + pluie (bruit faible) ; humidité du sol |
| Piézomètre | profondeur de nappe |
| Tour à flux (Eddy) | flux net de CO₂, dont un stock carbone *estimé* qui dérive doucement |
| Capteur acoustique + Piège photo | indice de faune (deux canaux fusionnés) |

Chaque capteur ajoute du **bruit gaussien** à la vérité du modèle — un clic en scène ouvre un panneau qui affiche la mesure du jour et le modèle de bruit (σ). Le point pédagogique : rendre **visible l'incertitude de mesure** propre à chaque techno, au lieu d'afficher des valeurs « propres » trompeuses.

Et surtout, **primauté du capteur** : les alertes seuillent la **mesure bruitée** (profondeur lue au piézomètre, stock estimé par la tour, indice de faune mesuré), *pas* la vérité interne du modèle. C'est ce qui distingue un digital twin honnête d'un jeu vidéo qui tricherait avec l'état caché.

→ *détail (modèles de bruit, seuils, chaînes capteur→événement) : [`10_MOTEUR-KPI-DECISION.md`](refonte/10_MOTEUR-KPI-DECISION.md)*

---

## 6. Le moteur de décision — dérivé du modèle

Quand un capteur détecte quelque chose (sécheresse, anomalie faune, carbone bas, rentabilité décrochée), le moteur propose une action. La sélection n'est **pas** une table de coefficients figés : pour l'événement, le moteur construit les leviers faisables, **simule chacun en avant** sur une copie de l'état (le vrai moteur, à 2 horizons, avec une fourchette météo), et garde celui qui sert le mieux **l'objectif de l'agriculteur**.

Deux honnêtetés importantes :
- **Reco ⊆ leviers** : tout ce qu'une recommandation propose, tu peux aussi le faire toi-même au slider. Pas de bouton magique.
- **Contre-recommandations assumées** : quand la rentabilité s'effondre, le moteur propose honnêtement de *ré-intensifier* ou d'arracher — dans la limite d'un garde-fou biodiversité. Il ne postule pas que « plus vert = toujours mieux ».

Les recos **gagnant-gagnant** s'affichent en **popup proactif** ; les **compromis** attendent dans une **liste passive** que tu consultes si tu veux. Tu valides, tu ignores, ou tu reportes.

→ *détail (projection d'issues, fonction-objectif marge-risque, surfaçage) : [`10_MOTEUR-KPI-DECISION.md`](refonte/10_MOTEUR-KPI-DECISION.md)*

---

## 7. Le fantôme, et « l'apport de la techno »

Pour mesurer ce que valent *tes* décisions, une **seconde simulation** tourne en parallèle : le **fantôme**. Même graine aléatoire, même climat, mêmes aides — mais les décisions de l'agriculteur **figées à leur valeur de départ**. Toute divergence entre le réel et le fantôme vient donc *uniquement* de ce que tu as changé.

Le Hero KPI **« apport de la techno »**, c'est cet écart, en euros nets cumulés (moins les investissements). Positif si ta stratégie informée rapporte plus qu'elle ne coûte.

**La nuance honnête** (et elle compte) : ce chiffre mesure le **gain marginal de la précision**, pas une preuve qu'il *faut* des capteurs pour bien cultiver. On peut conduire un bocage de façon responsable à l'œil et sans instrumentation. Ce que la donnée achète, c'est l'**optimisation millimétrée** de chaque levier — la couche Industrie 4.0 par-dessus une pratique déjà saine. Le twin est une loupe sur ce gain marginal, pas un argumentaire de vente de capteurs.

→ *détail (baseline gelée, valeur nette, déterminisme du fantôme) : [`08_MODELE.md` §10](refonte/08_MODELE.md) · [`10_MOTEUR-KPI-DECISION.md`](refonte/10_MOTEUR-KPI-DECISION.md)*

---

## 8. Les aides publiques, intégrées

La rentabilité affichée inclut explicitement l'**amortisseur principal du revenu agricole français** : DPB, paiement redistributif, écorégime, bonus haies PAC, PSE (paiement pour services environnementaux), MAEC, et le crédit carbone. Sans ces paiements, la plupart des fermes céréalières seraient déficitaires — et c'est précisément ce que le modèle rend visible : **l'écologie devient rentable quand on la monétise.**

→ *détail (montants, sources Légifrance/Chambres d'agriculture) : [`08_MODELE.md` §6](refonte/08_MODELE.md)*

---

## 9. Déterminisme et honnêteté

Deux garde-fous structurels :
- **Déterminisme** : même graine + mêmes décisions → exactement le même run. Tout l'aléa passe par un générateur seedé à sous-flux séparés (météo, capteurs, faune), ce qui rend chaque comparaison réelle/fantôme propre.
- **Primauté du capteur** (§5) : aucun élément visuel n'est piloté par le calendrier. Un oiseau qui passe = un indice de biodiversité mesuré au-dessus de son seuil. Une teinte de prairie = une humidité du sol. Jamais « c'est l'automne, donc des feuilles tombent ».

---

## 10. Limites honnêtes

Assumées, pour éviter toute survalorisation :

- **Mono-culture représentative** : le « champ » est une culture annuelle calibrée sur le blé (rotation blé/colza assumée dans la narration et via le levier prairie), pas une rotation simulée explicitement.
- **Biodiversité = 3 facteurs** ; un 4ᵉ (diversité du paysage) est au backlog.
- **Phénologie simplifiée** : pas de semis/récolte explicites au-delà de la fenêtre de croissance.
- **Pas de santé végétale** (pathogènes, ravageurs) ni d'aléa de mortalité des plantations.
- **Hydrologie schématique**, non validée hydrologiquement.
- **Calibration de niveau moyen** : ordres de grandeur sourcés (INRAE, Solagro, Agreste, Arvalis, COMIFER, Météo-France, AFAC, Légifrance), mais pas d'audit terrain.

→ *liste complète et hooks d'implémentation : [`../docs/BACKLOG.md`](BACKLOG.md)*

---

## 11. Pour aller plus loin

Tu veux le calcul exact et les sources de chaque chiffre ? Le **niveau technique** :

- [**`refonte/08_MODELE.md`**](refonte/08_MODELE.md) — le modèle biophysique complet : stocks, flux, couplages, leviers, KPI, et le **tableau de paramètres sourcé** (§8).
- [**`refonte/10_MOTEUR-KPI-DECISION.md`**](refonte/10_MOTEUR-KPI-DECISION.md) — la boucle de simulation, le calcul des KPI, le moteur de recommandation.
- [**`refonte/11_VERIFICATION-MATHS.md`**](refonte/11_VERIFICATION-MATHS.md) — la vérification mathématique (« gate papier ») : analyse dimensionnelle, équilibres, stabilité, optima intérieurs.

Et pour le **logiciel** plutôt que la science :

- [`ARCHITECTURE.md`](ARCHITECTURE.md) — les 5 couches, le graphe d'asmdef, le flux de données.
- [`DECISIONS.md`](DECISIONS.md) — le journal des décisions de design (ADR).
