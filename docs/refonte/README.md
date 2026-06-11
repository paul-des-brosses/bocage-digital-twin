# Le modèle — spec technique (niveau 3)

Ce dossier contient la **spec autoritaire du modèle** du digital twin : le
modèle biophysique, le moteur de KPI/décision, et sa vérification mathématique.
Depuis le **cutover S5** (2026-06-11), **la refonte EST le code** : ces
documents décrivent le modèle *actuel*, plus une cible. La vue d'ensemble
vulgarisée (niveau 2) vit dans
[`../SIMULATION_OVERVIEW.md`](../SIMULATION_OVERVIEW.md), qui pointe ici pour le
détail. L'ancien [`../CALIBRATION.md`](../CALIBRATION.md) est conservé comme
**archive pré-refonte**.

## Les documents

| Doc | Contenu |
|---|---|
| [`08_MODELE.md`](08_MODELE.md) | Le **modèle biophysique** : stocks, flux (eau FAO-56, carbone ICBM 2 pools, azote explicite), couplages, leviers, KPI, objectif, paramètres sourcés. Décisions verrouillées en §13. |
| [`10_MOTEUR-KPI-DECISION.md`](10_MOTEUR-KPI-DECISION.md) | Le **moteur** : boucle de simulation (tick), calcul des KPI, moteur de recommandation (recherche de dose optimale, objectif marge-risque, surfaçage). Leviers MVP en Partie D. |
| [`11_VERIFICATION-MATHS.md`](11_VERIFICATION-MATHS.md) | Le **« gate papier »** : analyse dimensionnelle, équilibres, stabilité, signes de sensibilité, optima intérieurs. Verdict : le modèle passe. |

## Pourquoi une refonte

L'ancien modèle est une **« étoile centrée sur le profit »** : le climat ne
frappe fort que le rendement/coût, tandis que nappe, carbone et biodiversité
sont découplés du climat (d'où le symptôme « 10 ans à −50 % de pluie et tout
va bien sauf l'onglet éco »). La refonte le transforme en **réseau** où une
sécheresse se propage en cascade, via le carrefour manquant : **`θ`, la
réserve en eau du sol racinaire** (bucket FAO-56, ETP de Hargreaves), avec la
boucle de rétroaction `rendement → résidus → carbone → réserve en eau`.

## Principes verrouillés

- **Primauté du capteur** : tout visuel/alerte dérive d'une mesure ou d'une
  variable d'état.
- **Reco ⊆ leviers** : tout ce qu'une popup de recommandation propose est
  aussi actionnable directement.
- **« Optimiser, pas moraliser »** : chaque levier a un downside réel → un
  optimum intérieur (démontré au doc 11), pas une solution de coin.
- **6 leviers MVP** : fertilisation azotée, IFT/pesticides, travail du sol,
  couverts, gestion flore/haies, part de prairie (revenu fourrager léger).

## Statut

Spec **implémentée et vérifiée** : couches 01-04 codées et validées par une
suite de tests EditMode headless ; la calibration de la réponse azotée a été
refaite sur Arvalis/COMIFER/INRAE (cf [`08_MODELE.md`](08_MODELE.md) §5.5) ;
l'ancien modèle a été supprimé au cutover S5. Le modèle vivant est celui décrit
ici.

## Données de calibration

La météo est **générée** par un générateur stochastique seedé (chaîne de
Markov occurrence + AR(1) température) **calibré** sur le relevé Météo-France
de Tourouvre-au-Perche (`Meteo_Tourouvre.csv`, racine du repo), via
`tools/extract_weather_normals.py` — à étendre pour produire les transitions
de Markov, l'amplitude diurne (`TN/TX`) et les paramètres de température.
