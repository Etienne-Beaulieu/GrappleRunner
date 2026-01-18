using UnityEngine;
using System;

// Classe qui gere mon chronometre
public class Chronometre : MonoBehaviour
{
    // Parametre initiaux
    [Header("Parametres")]
    [SerializeField] private float tempsDepart = 90f;
    [SerializeField] private float penaliteMort = 10f;

    // Le temps restant
    private float tempsRestant;

    // Bool estActif
    private bool estActif = false;

    // Evenements
    public event Action OnTempsEcoule;
    public event Action<float> OnChangementTemps;

    // GETTERS / SETTERS
    public float TempsRestant => tempsRestant;
    public bool EstActif => estActif;

    // SINGLETON
    public static Chronometre Instance { get; private set; }

    // === UNITY ===

    /// <summary>
    /// A la premiere frame
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
    }

    /// <summary>
    /// Initialise le temps restant au depart
    /// </summary>
    void Start()
    {
        tempsRestant = tempsDepart;
    }

    /// <summary>
    /// Dans update on gere le temps du chronometre
    /// </summary>
    void Update()
    {
        if (!estActif) return;

        tempsRestant -= Time.deltaTime;

        // On invoque l'evenement pour updater le chrono
        OnChangementTemps?.Invoke(tempsRestant);

        // Si le temps est fini on lance l'evenement pour terminer la partie et ouvrir le menu de fin
        if (tempsRestant <= 0)
        {
            OnChangementTemps?.Invoke(tempsRestant);
            OnTempsEcoule?.Invoke();
        }
    }

    /// <summary>
    /// Detruit l'instance du singleton au besoin
    /// </summary>
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // === FONCTIONS ===

    /// <summary>
    /// Fonction pour mettre le temps du chronometre a celui choisi dans le menu principal
    /// </summary>
    /// <param name="nouveauTemps">Le temps pour reinitialiser</param>
    public void DefinirTempsDepart(float nouveauTemps)
    {
        tempsDepart = nouveauTemps;
        tempsRestant = nouveauTemps;

        OnChangementTemps?.Invoke(tempsRestant);
    }

    /// <summary>
    /// Partir le chronometre
    /// </summary>
    public void DemarrerChronometre()
    {
        estActif = true;
    }

    /// <summary>
    /// Reinitialiser le chronometre
    /// </summary>
    public void Reinitialiser()
    {
        tempsRestant = tempsDepart;
        estActif = false;

        OnChangementTemps?.Invoke(tempsRestant);
    }

    // === MODIFICATIONS ===

    /// <summary>
    /// Ajouter du temps au chronometre
    /// </summary>
    /// <param name="temps">Temps a ajouter</param>
    public void AjouterTemps(float temps)
    {
        tempsRestant += temps;

        OnChangementTemps?.Invoke(tempsRestant);
    }

    /// <summary>
    /// Appliquer la penalite lorsque le joueur meurt
    /// </summary>
    public void AppliquerPenalite()
    {
        tempsRestant -= penaliteMort;

        OnChangementTemps?.Invoke(tempsRestant);
    }

    // === PAUSE / REPRENDRE ===

    /// <summary>
    /// Mettre le chronometre en pause pour les menus
    /// </summary>
    public void MettrePause()
    {
        estActif = false;
    }

    /// <summary>
    /// Repartir le chronometre pour sortie de menu
    /// </summary>
    public void Reprendre()
    {
        estActif = true;
    }
}