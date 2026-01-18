using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Classe qui gere mon interface
public class ControleurInterface : MonoBehaviour
{
    // Dependances
    [Header("Dependances")]
    [SerializeField] private Chronometre chronometre;

    // Composants de l'interface
    [Header("Interface")]
    [SerializeField] private TextMeshProUGUI texteTemps;
    [SerializeField] private TextMeshProUGUI texteNomJoueur;
    [SerializeField] private Image barreRechargeGrappin;
    [SerializeField] private GameObject panneauInstructions;

    // Composants du menu principal
    [Header("Menu Principal")]
    [SerializeField] private TMP_InputField champNomJoueur;
    [SerializeField] private Slider curseurTemps;
    [SerializeField] private TextMeshProUGUI texteTempsDepart;

    // Composants du menu de fin
    [Header("Menu Fin de Partie")]
    [SerializeField] private TextMeshProUGUI texteTempsTotal;

    // Valeurs initiales du menu principal
    private string nomJoueur = "JOUEUR";
    private float tempsDepart = 90f;

    // GETTERS
    public float TempsDepart => tempsDepart;
    public string NomJoueur => nomJoueur;

    // === UNITY ===

    /// <summary>
    /// Au depart
    /// </summary>
    void Start()
    {
        // S'abonner aux evenement du chronometre
        if (Chronometre.Instance != null)
        {
            Chronometre.Instance.OnChangementTemps += UpdateAffichageTemps;
            Chronometre.Instance.OnTempsEcoule += AfficherStatistiquesFinales;
        }

        // Configuration du curseur de temps
        if (curseurTemps != null)
        {
            curseurTemps.onValueChanged.AddListener(EnregistrerTempsDepart);
            EnregistrerTempsDepart(curseurTemps.value);
        }

        // Instructions cachees par defaut
        if (panneauInstructions != null)
        {
            panneauInstructions.SetActive(false);
        }
    }

    /// <summary>
    /// OnDestroy pour se desabonner
    /// </summary>
    void OnDestroy()
    {
        if (Chronometre.Instance != null)
        {
            Chronometre.Instance.OnChangementTemps -= UpdateAffichageTemps;
            Chronometre.Instance.OnTempsEcoule -= AfficherStatistiquesFinales;
        }

        if (curseurTemps != null)
        {
            curseurTemps.onValueChanged.RemoveListener(EnregistrerTempsDepart);
        }
    }


    // === FONCTIONS ===

    /// <summary>
    /// Fonction pour formater le temps et eviter les repetitions
    /// </summary>
    /// <param name="temps">Temps recu pour formatage</param>
    /// <returns></returns>
    private string FormaterTemps(float temps)
    {
        int minutes = Mathf.FloorToInt(temps / 60f);
        int secondes = Mathf.FloorToInt(temps % 60f);

        return $"{minutes:00}:{secondes:00}";
    }

    // === INSTRUCTIONS ===

    /// <summary>
    /// Fonction a appeller pour les instructions avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnInstructions(InputAction.CallbackContext contexte)
    {
        if (!contexte.performed) return;

        if (panneauInstructions != null)
        {
            Instructions(!panneauInstructions.activeSelf);
        }
    }

    /// <summary>
    /// Fonction gerer les instructions
    /// </summary>
    /// <param name="afficher">Bool recu pour afficher les instructions</param>
    public void Instructions(bool afficher)
    {
        if (panneauInstructions != null)
        {
            panneauInstructions.SetActive(afficher);
        }
    }

    // === INTERFACE ===

    /// <summary>
    /// Fonction pour mettre a jour le chronometre dans l'interface
    /// </summary>
    /// <param name="temps">Temps recu pour update le chrono a l'ecran</param>
    public void UpdateAffichageTemps(float temps)
    {
        if (texteTemps != null)
        {
            texteTemps.text = $"Temps: {FormaterTemps(temps)}";

            // Couleur rouge si il reste moins de 10 secondes
            if (temps < 10f)
            {
                texteTemps.color = Color.red;
            }

            // Couleur jaune si il reste moins de 10 secondes
            else if (temps < 30f)
            {
                texteTemps.color = Color.yellow;
            }

            // Couleur blanche sinon
            else
            {
                texteTemps.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Fonction pour mettre a jour la barre de cooldown dans l'interface
    /// </summary>
    /// <param name="pourcentage">Pourcentage recu du grappin pour update la barre de cooldown</param>
    public void UpdateCooldownGrappin(float pourcentage)
    {
        if (barreRechargeGrappin != null)
        {
            // Rempli la barre en fonction du pourcentage recu
            barreRechargeGrappin.fillAmount = Mathf.Clamp01(pourcentage);

            // Couleur verte si moins de 100%
            if (pourcentage >= 1f)
            {
                barreRechargeGrappin.color = Color.green;
            }

            // Couleur jaune si 50%
            else if (pourcentage >= 0.5f)
            {
                barreRechargeGrappin.color = Color.yellow;
            }

            // Couleur rouge sinon
            else
            {
                barreRechargeGrappin.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Fonction pour afficher le temps final dans le menu de fin
    /// </summary>
    public void AfficherStatistiquesFinales()
    {
        float tempsFin = 0;

        if (ControleurJeu.Instance != null)
        {
            tempsFin = ControleurJeu.Instance.TempsTotal;
        }

        if (texteTempsTotal != null)
        {
            texteTempsTotal.text = $"Temps de survie: {FormaterTemps(tempsFin)}";
        }
    }

    // === ENREGISTREMENTS ===

    /// <summary>
    /// Fonction pour mettre le temps choisi
    /// </summary>
    /// <param name="temps">Temps voulu recu pour la partie</param>
    void EnregistrerTempsDepart(float temps)
    {
        tempsDepart = temps;

        if (texteTempsDepart != null)
        {
            texteTempsDepart.text = $"Temps de départ: {FormaterTemps(temps)}";
        }
    }

    /// <summary>
    /// Fonction pour mettre le nom choisi
    /// </summary>
    public void EnregistrerNomJoueur()
    {
        if (champNomJoueur != null && !string.IsNullOrEmpty(champNomJoueur.text))
        {
            nomJoueur = champNomJoueur.text;
        }

        // Mettre à jour l'affichage du nom
        if (texteNomJoueur != null)
        {
            texteNomJoueur.text = nomJoueur;
        }
    }
}