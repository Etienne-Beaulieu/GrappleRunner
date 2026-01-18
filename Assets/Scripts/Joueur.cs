using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using EtatsJoueur;

// J'utilise un charactercontroller parce que mon emprunt de grappin rabibocher avec l'ia le necessitait,
// sinon c'etait un rigidbody et ca marchait bien comme ca. Alors j'ai adapte certaines fonctions pour
// l'utiliser et vu que c'etait bien pratique

// Classe qui gere mon joueur
[RequireComponent(typeof(AudioSource)), RequireComponent(typeof(CharacterController))]
public class Joueur : MonoBehaviour
{
    // Dependances publiques
    [Header("Dependances")]
    [SerializeField] private Transform transformCamera;
    [SerializeField] private Transform boutGrappin;
    [SerializeField] private Animator controleurAnimation;

    // Dependances prives
    private CharacterController controleurCharacter;
    private AudioSource sourceAudio;

    // Parametres de mouvement
    [Header("Mouvement")]
    [SerializeField] public float vitesse = 12f;
    [SerializeField] public float forceSaut = 12f;
    [SerializeField] public float gravite = -16f;

    // Parametres de la souris
    [Header("Parametres souris")]
    [SerializeField] private float sensibiliteHorizontal = 80f;
    [SerializeField] private float sensibiliteVertical = 80f;
    [SerializeField] private float limiteVerticale = 80f;

    // Parametres de mort
    [Header("Parametres mort")]
    [SerializeField] public float hauteurMort = -10f;

    // Effets
    [Header("Effets")]
    [SerializeField] private AudioClip sonMort;

    // Composants liés au grappin
    public MouvementGrappin mouvementGrappin { get; private set; }
    public Grappin grappin { get; private set; }

    // Inputs
    private Vector2 inputMouvement;
    private Vector2 inputRegard;
    private float rotationVerticale;

    // Mouvement
    private Vector3 vitesseActuelle;

    // Etat de la frame precedente
    private EtatJoueur etatPrecedent;

    // Etat de la frame suivante
    private EtatJoueur etatSuivant;

    // Bools pour la machine a etats
    private bool enSaut = false;
    private bool meurt = false;
    private bool controleActif = true;

    // Parametre de respawn
    private Vector3 dernierCheckpoint;

    // GETTERS / SETTERS
    public Transform TransformCamera => transformCamera;
    public Transform BoutGrappin => boutGrappin;
    public bool EnMouvement => inputMouvement.sqrMagnitude > 0.01f;
    public bool EnSaut { get => enSaut; set => enSaut = value; }
    public bool EnGrappin => mouvementGrappin.Actif;
    public bool Meurt => meurt;
    public bool ControleActif { get => controleActif; set => controleActif = value; }
    public Vector2 ObtenirInputDeplacement() => inputMouvement;
    public Vector3 VitesseActuelle => vitesseActuelle;
    public CharacterController ControleurCharacter => controleurCharacter;
    public void DefinirVitesseY(float nouvelleVitesseY)
    {
        vitesseActuelle.y = nouvelleVitesseY;
    }

    // === UNITY ===

    /// <summary>
    /// A la premiere frame on assigne les composantes et on lance l'etat d'attente
    /// </summary>
    private void Awake()
    {
        controleurCharacter = GetComponent<CharacterController>();
        sourceAudio = GetComponent<AudioSource>();

        // Assigner le grappin en emprunt
        mouvementGrappin = GetComponent<MouvementGrappin>();
        mouvementGrappin.Initialiser(controleurCharacter, transform, this);
        grappin = GetComponent<Grappin>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        etatSuivant = new EtatAttente();
    }

    /// <summary>
    /// Dans update on gere la machine a etat et le mouvement
    /// </summary>
    private void Update()
    {
        Regarder();
        Deplacer(inputMouvement);

        // Besoin de cette ligne aussi comme dans deplacer
        mouvementGrappin.Tick(inputMouvement);

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
    }

    // === INPUT CALLBACKS ===

    /// <summary>
    /// Fonction a appeller pour bouger avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnDeplacement(InputAction.CallbackContext contexte)
    {
        if (!controleActif) return;

        inputMouvement = contexte.ReadValue<Vector2>();
    }

    /// <summary>
    /// Fonction a appeller pour regarder avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnRegard(InputAction.CallbackContext contexte)
    {
        if (!controleActif) return;

        inputRegard = contexte.ReadValue<Vector2>();
    }

    /// <summary>
    /// Fonction a appeller pour sauter avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnSaut(InputAction.CallbackContext contexte)
    {
        if (!controleActif) return;

        if (contexte.started)
        {
            enSaut = true;
        }

        else if (contexte.canceled)
        {
            enSaut = false;
        }
    }

    /// <summary>
    /// Fonction a appeller pour sauter vers un point avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnGrappinSauterVersPoint(InputAction.CallbackContext contexte)
    {
        if (!controleActif) return;

        if (contexte.started && grappin != null)
        {
            grappin.TryGrapple(transformCamera.position, transformCamera.forward, MouvementGrappin.ModeGrappin.SauterVersPoint);
        }
    }

    /// <summary>
    /// Fonction a appeller pour se balancer avec le input system
    /// </summary>
    /// <param name="contexte">Contexte du input system</param>
    public void OnGrappinBalancer(InputAction.CallbackContext contexte)
    {
        if (!controleActif) return;

        if (contexte.started && grappin != null)
        {
            grappin.TryGrapple(transformCamera.position, transformCamera.forward, MouvementGrappin.ModeGrappin.Balancer);
        }

        if (contexte.canceled && grappin != null)
        {
            grappin.ArreterGrappin();
        }
    }

    // === MOUVEMENT ===

    /// <summary>
    /// Fonction pour gerer le mouvement, ici quelques lignes sugeree par l'ia sinon mon grappin marchait pas
    /// </summary>
    /// <param name="input">Input du input system</param>
    private void Deplacer(Vector2 input)
    {
        if (controleurCharacter == null || !controleurCharacter.enabled) return;

        // Calcul direction horizontale
        Vector3 direction = transform.forward * input.y + transform.right * input.x;
        Vector3 directionHorizontale = direction.normalized * vitesse;
        vitesseActuelle.x = directionHorizontale.x;
        vitesseActuelle.z = directionHorizontale.z;

        // Appliquer la gravite geree dans le grappin
        mouvementGrappin.AppliquerGravite(ref vitesseActuelle);

        // Mouvement total (on doit ajouter la vitesse du grappin au joueur)
        Vector3 deplacement = vitesseActuelle + mouvementGrappin.VitesseAjoutee + mouvementGrappin.VitesseExterne;
        controleurCharacter.Move(deplacement * Time.deltaTime);
    }

    /// <summary>
    /// Fonction pour gerer le regard
    /// </summary>
    private void Regarder()
    {
        if (!controleActif) return;

        float rotationHorizontale = inputRegard.x * sensibiliteHorizontal * Time.deltaTime;
        float rotationVerticaleDelta = inputRegard.y * sensibiliteVertical * Time.deltaTime;

        transform.Rotate(Vector3.up * rotationHorizontale);

        rotationVerticale -= rotationVerticaleDelta;
        rotationVerticale = Mathf.Clamp(rotationVerticale, -limiteVerticale, limiteVerticale);

        transformCamera.localRotation = Quaternion.Euler(rotationVerticale, 0, 0);
    }

    /// <summary>
    /// Fonction pour gerer sauter
    /// </summary>
    public void Sauter()
    {
        vitesseActuelle.y = forceSaut;
    }

    // === GRAPPIN ===

    /// <summary>
    /// Methode pour annuler le mouvement du joueur lorsquon appelle JumpToPoint et regler mon bug
    /// </summary>
    public void AnnulerToutMouvement()
    {
        vitesseActuelle = Vector3.zero;
    }

    /// <summary>
    /// Pour reset l'input et avoir le comportement je voulais avec sauter vers point
    /// </summary>
    public void ResetInput()
    {
        inputMouvement = Vector2.zero;
    }

    // === MORT ===

    /// <summary>
    /// Lorsque le joueur entre en collision avec le sol et n'est pas entrain de mourir on enregistre le spawn point
    /// </summary>
    /// <param name="hit">Parametre hit de la fonction du charactercontroler</param>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (controleurCharacter.isGrounded && !meurt && hit.point.y > hauteurMort + 1f)
        {
            dernierCheckpoint = hit.point;
        }
    }

    /// <summary>
    /// Fonction mourir qui joue un son un demarre la coroutine de respawn
    /// </summary>
    public void Mourir()
    {
        // Si on meurt deja on return pour ne pas appliquer plusieures penalites de temps
        if (meurt) return;

        meurt = true;

        // Joueur le son
        if (sonMort != null && sourceAudio != null)
        {
            sourceAudio.PlayOneShot(sonMort);
        }

        Chronometre.Instance?.AppliquerPenalite();

        StartCoroutine(ReapparaitreCoroutine());
    }

    /// <summary>
    /// Coroutine pour le respawn du joueur et gerer meurt pour eviter les penalites multiples
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReapparaitreCoroutine()
    {
        // On attend le temps voulu
        yield return new WaitForSeconds(0.5f);

        if (controleurCharacter != null)
        {
            controleurCharacter.enabled = false;
        }

        transform.position = dernierCheckpoint + Vector3.up * 0.5f;

        // On attend une frame pour donner le temps au joueur de spawn
        yield return null;

        if (controleurAnimation != null)
        {
            controleurAnimation.SetTrigger("death");
        }

        if (controleurCharacter != null)
        {
            controleurCharacter.enabled = true;
        }

        meurt = false;
    }
}