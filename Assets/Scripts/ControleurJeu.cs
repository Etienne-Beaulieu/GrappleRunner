using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using EtatsMenu;

// Classe qui gere mon jeu
public class ControleurJeu : MonoBehaviour
{
    // Dependances
    [Header("Dependances")]
    [SerializeField] private Chronometre chronometre;
    [SerializeField] private ControleurInterface controleurInterface;
    [SerializeField] private GameObject menuPrincipal;
    [SerializeField] private GameObject menuPause;
    [SerializeField] private GameObject menuFinPartie;
    [SerializeField] private GameObject interfaceJeu;
    [SerializeField] public Joueur joueur;

    // Etat de la frame precedente
    private EtatMenu etatPrecedent;

    // Etat de la frame suivante
    private EtatMenu etatSuivant;

    // Temps final
    private float tempsTotal = 0f;

    // Bools pour la machine a etats
    private bool demandePause = false;
    private bool demandeReprise = false;
    private bool demandeDebut = false;
    private bool demandeFin = false;

    // GETTERS / SETTERS
    public float TempsTotal => tempsTotal;
    public GameObject MenuPrincipal => menuPrincipal;
    public GameObject MenuPause => menuPause;
    public GameObject MenuFinPartie => menuFinPartie;
    public GameObject InterfaceJeu => interfaceJeu;
    public Chronometre Chronometre => chronometre;
    public ControleurInterface ControleurInterface => controleurInterface;
    public bool DemandePause { get => demandePause; set => demandePause = value; }
    public bool DemandeReprise { get => demandeReprise; set => demandeReprise = value; }
    public bool DemandeDebut { get => demandeDebut; set => demandeDebut = value; }
    public bool DemandeFin { get => demandeFin; set => demandeFin = value; }

    // SINGLETON
    public static ControleurJeu Instance { get; private set; }

    // === UNITY ===

    /// <summary>
    /// A la premiere frame on desactive tous les menus et on lance l'etat menu principal
    /// </summary>
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DesactiverTousLesMenus();

        etatSuivant = new EtatMenuPrincipal();
    }

    /// <summary>
    /// S'abonner a l'evenement de fin de temps pour terminer la partie au depart
    /// </summary>
    void Start()
    {
        if (Chronometre.Instance != null)
        {
            Chronometre.Instance.OnTempsEcoule += GererTempsEcoule;
        }
    }

    /// <summary>
    /// Dans update on gere la machine a etat
    /// </summary>
    void Update()
    {
        if (etatPrecedent != etatSuivant)
        {
            etatSuivant.EntrerEtat(this);
        }

        etatPrecedent = etatSuivant;
        etatSuivant = etatSuivant.ExecuterEtat(this);

        if (etatPrecedent != etatSuivant)
        {
            etatPrecedent.SortirEtat(this);
        }

        // Compter le temps total seulement dans EtatEnJeu
        if (etatPrecedent is EtatEnJeu)
        {
            tempsTotal += Time.deltaTime;
        }
    }

    /// <summary>
    /// Detruit l'instance du singleton au besoin et se desabonne de l'evenement
    /// </summary>
    void OnDestroy()
    {
        if (Chronometre.Instance != null)
        {
            Chronometre.Instance.OnTempsEcoule -= GererTempsEcoule;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // === FONCTIONS ===

    /// <summary>
    /// Demander la fin du jeu si temps ecoule
    /// </summary>
    private void GererTempsEcoule()
    {
        demandeFin = true;
    }

    /// <summary>
    /// Fonction pour desactiver tous les menus c'est pratique dans EtatEnJeu et a la premiere frame
    /// </summary>
    public void DesactiverTousLesMenus()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(false);
        if (interfaceJeu != null) interfaceJeu.SetActive(false);
        if (menuPause != null) menuPause.SetActive(false);
        if (menuFinPartie != null) menuFinPartie.SetActive(false);
    }

    // === INPUT CALLBACKS ===

    /// <summary>
    /// Fonction a appeller pour pause avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnPause(InputAction.CallbackContext contexte)
    {
        if (!contexte.performed) return;

        if (etatPrecedent is EtatEnJeu)
        {
            demandePause = true;
        }

        else if (etatPrecedent is EtatMenuPause)
        {
            demandeReprise = true;
        }
    }

    // === BOUTONS ===

    /// <summary>
    /// Fonction pour le bouton demarrer
    /// </summary>
    public void BoutonDemarrer()
    {
        // Met le temps total a zero d'un coup que ce n'est pas la premiere partie
        tempsTotal = 0f;

        // Enregistrer le nom du joueur
        if (controleurInterface != null)
        {
            controleurInterface.EnregistrerNomJoueur();
        }

        if (chronometre != null)
        {
            chronometre.Reinitialiser();

            // Appliquer le temps choisi par le joueur
            if (controleurInterface != null)
            {
                float tempsChoisi = controleurInterface.TempsDepart;
                chronometre.DefinirTempsDepart(tempsChoisi);
            }

            chronometre.DemarrerChronometre();
        }

        // Ca me semblais la facon la plus propre d'utiliser ce bool directement dans le menu principal pour entrer en EtatEnJeu
        demandeDebut = true;
    }

    /// <summary>
    /// Fonction pour le bouton reprise
    /// </summary>
    public void BoutonReprise()
    {
        demandeReprise = true;
    }

    /// <summary>
    /// Fonction pour le bouton recommencer
    /// </summary>
    public void BoutonRecommencer()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Fonction pour le bouton quitter
    /// </summary>
    public void BoutonQuitter()
    {
        Application.Quit();

        #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}