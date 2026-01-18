using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MouvementGrappin : MonoBehaviour
{
    public enum ModeGrappin { SauterVersPoint, Balancer }

    [Header("Parametres Controle")]
    [SerializeField] private float controleMilieuAir = 8f;
    [SerializeField] private float controleApresSaut = 15f;

    [Header("Parametres Balancer")]
    [SerializeField] private float graviteBalancer = 18f;
    [SerializeField] private float amortissement = 0.9985f;
    [SerializeField] private float forceContrainte = 0.98f;
    [SerializeField] private float vitesseMonteeMax = 40f;
    [SerializeField] private float multiplicateurBigAir = 2.5f;
    [SerializeField] private float boostVerticalMinimum = 12f;
    [SerializeField] private float conversionMomentumHorizontal = 0.4f;
    [SerializeField] private float seuilVitesseBonus = 15f;

    [Header("Gravite douce")]
    [SerializeField] private float graviteDouce = -2f;
    [SerializeField] private float dureeGraviteDouce = 1.2f;
    [SerializeField] private float vitesseChuteMaxApresBigAir = -20f;

    // Dependances
    private CharacterController controleurCharacter;
    private Transform transformJoueur;
    private Joueur joueur;

    // Etat
    private bool actif;
    private ModeGrappin mode;
    private Vector3 pointGrapple;

    // Saut
    private Vector3 vitesseSaut;
    private float timerSecuriteSaut;
    private const float DUREE_MAX_SAUT = 5f;

    // Balancer
    private float longueurCorde;
    private Vector3 vitesseBalancer;

    // Liberation
    private bool graviteReduite;
    private float timerGraviteReduite;
    private Vector2 dernierInputSwing;
    private bool sortieDeBigAir;

    // Vitesse externe
    private Vector3 vitesseExterne = Vector3.zero;
    private Vector3 vitesseAjoutee;
    private float tempsDecayExterne;

    // GETTERS / SETTERS
    public Vector3 VitesseExterne { get => vitesseExterne; set => vitesseExterne = value; }
    public Vector3 VitesseAjoutee { get => vitesseAjoutee; set => vitesseAjoutee = value; }
    public bool Actif => actif;

    public float ObtenirModificateurGravite()
    {
        if (mode == ModeGrappin.Balancer && graviteReduite && timerGraviteReduite > 0f)
            return graviteDouce;

        return joueur.gravite;
    }

    // === INITIALISATION ===

    public void Initialiser(CharacterController controleur, Transform transform, Joueur joueurRef)
    {
        controleurCharacter = controleur;
        transformJoueur = transform;
        joueur = joueurRef;
    }

    // === FONCTIONS PUBLIQUES ===

    public void DemarrerBalancer(Vector3 cible)
    {
        InitialiserGrappin(cible, ModeGrappin.Balancer);

        longueurCorde = Vector3.Distance(transformJoueur.position, pointGrapple);
        Vector3 vitesseInit = joueur.VitesseActuelle;
        Vector3 dirCorde = (transformJoueur.position - pointGrapple).normalized;
        Vector3 vitesseRadiale = Vector3.Project(vitesseInit, dirCorde);
        vitesseBalancer = vitesseInit - vitesseRadiale;

        if (joueur.EnMouvement)
        {
            Vector3 tangent = Vector3.Cross(dirCorde, Vector3.up).normalized;
            float inputSwing = joueur.ObtenirInputDeplacement().x;
            vitesseBalancer += tangent * inputSwing * controleMilieuAir;
        }
    }

    public void SauterParabolique(Vector3 cible, float hauteurTrajectoire, float multiplicateurVitesse = 1f)
    {
        InitialiserGrappin(cible, ModeGrappin.SauterVersPoint);
        vitesseSaut = CalculerVitesseSaut(transformJoueur.position, pointGrapple, hauteurTrajectoire) * multiplicateurVitesse;
    }

    public void AppliquerSaut(Vector3 vitesse)
    {
        InitialiserGrappin(Vector3.zero, ModeGrappin.SauterVersPoint);
        vitesseSaut = vitesse;
    }

    public void ArreterGrappin()
    {
        if (!actif) return;

        if (mode == ModeGrappin.Balancer)
        {
            Vector3 momentumSortie = vitesseBalancer;
            Vector3 vitesseHorizontale = new Vector3(momentumSortie.x, 0f, momentumSortie.z);
            float vitesseHorizontaleMagnitude = vitesseHorizontale.magnitude;
            float bonusVertical = 0f;

            if (vitesseHorizontaleMagnitude > seuilVitesseBonus)
            {
                bonusVertical = (vitesseHorizontaleMagnitude - seuilVitesseBonus) * conversionMomentumHorizontal;
            }

            if (momentumSortie.y >= 0)
            {
                momentumSortie.y = Mathf.Clamp(
                    momentumSortie.y * multiplicateurBigAir + bonusVertical,
                    boostVerticalMinimum,
                    vitesseMonteeMax
                );
            }

            else
            {
                if (vitesseHorizontaleMagnitude > seuilVitesseBonus)
                {
                    momentumSortie.y = Mathf.Clamp(
                        boostVerticalMinimum + bonusVertical,
                        boostVerticalMinimum,
                        vitesseMonteeMax
                    );
                }
            }

            AppliquerVitesseExterne(momentumSortie, dureeGraviteDouce);
            graviteReduite = true;
            timerGraviteReduite = dureeGraviteDouce;
            sortieDeBigAir = true;

            vitesseBalancer = Vector3.zero;
            dernierInputSwing = Vector2.zero;
        }

        else if (mode == ModeGrappin.SauterVersPoint)
        {
            AppliquerVitesseExterne(vitesseSaut, 0.3f);
        }

        actif = false;
    }

    public void Tick(Vector2 input)
    {
        GererTimers();
        GererVitesseInterne(input);

        if (!actif) return;

        switch (mode)
        {
            case ModeGrappin.SauterVersPoint:
                GererSaut();
                break;

            case ModeGrappin.Balancer:
                dernierInputSwing = input;
                GererBalancer(input);
                break;
        }
    }

    public void AppliquerGravite(ref Vector3 vitesseActuelle)
    {
        if (mode == ModeGrappin.Balancer && actif) return;

        float graviteEffective = ObtenirModificateurGravite();

        if (controleurCharacter.isGrounded && vitesseActuelle.y < 0)
        {
            vitesseActuelle.y = -2f;
            sortieDeBigAir = false;
        }
        else
        {
            vitesseActuelle.y += graviteEffective * Time.deltaTime;

            if (sortieDeBigAir && vitesseActuelle.y < vitesseChuteMaxApresBigAir)
            {
                vitesseActuelle.y = vitesseChuteMaxApresBigAir;
            }
        }
    }

    // === FONCTIONS PRIVEES ===

    private void GererSaut()
    {
        if (controleurCharacter == null || !controleurCharacter.enabled) return;

        timerSecuriteSaut += Time.deltaTime;

        if (timerSecuriteSaut > DUREE_MAX_SAUT)
        {
            Debug.LogWarning("Saut forcé à s'arrêter (sécurité anti-gel)");
            ReinitialiserEtat();
            return;
        }

        vitesseAjoutee = vitesseSaut;
        vitesseSaut.y = Mathf.Max(vitesseSaut.y + Physics.gravity.y * Time.deltaTime, -50f);

        if (controleurCharacter.isGrounded)
        {
            ReinitialiserEtat();
        }
    }

    private void GererBalancer(Vector2 input)
    {
        if (controleurCharacter == null || !controleurCharacter.enabled) return;

        vitesseBalancer += Vector3.down * graviteBalancer * Time.deltaTime;
        Vector3 corde = transformJoueur.position - pointGrapple;

        if (corde.magnitude > 0.01f)
        {
            Vector3 tangent = Vector3.Cross(corde, Vector3.up).normalized;
            vitesseBalancer += tangent * input.x * controleMilieuAir * Time.deltaTime;
        }

        controleurCharacter.Move(vitesseBalancer * Time.deltaTime);
        corde = transformJoueur.position - pointGrapple;
        float longueur = corde.magnitude;

        if (longueur > longueurCorde)
        {
            Vector3 direction = corde / longueur;
            Vector3 correction = -direction * (longueur - longueurCorde) * forceContrainte;
            controleurCharacter.Move(correction);

            float vitesseRadiale = Vector3.Dot(vitesseBalancer, direction);
            if (vitesseRadiale > 0)
            {
                vitesseBalancer -= direction * vitesseRadiale;
            }
        }

        vitesseBalancer *= amortissement;
    }

    private void GererTimers()
    {
        if (graviteReduite)
        {
            timerGraviteReduite -= Time.deltaTime;
            if (timerGraviteReduite <= 0f)
            {
                graviteReduite = false;
            }
        }
    }

    private void GererVitesseInterne(Vector2 input)
    {
        vitesseAjoutee = Vector3.zero;

        if (tempsDecayExterne > 0f)
        {
            tempsDecayExterne -= Time.deltaTime;

            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 directionControle = (transformJoueur.forward * input.y + transformJoueur.right * input.x);
                vitesseExterne += directionControle * controleApresSaut * Time.deltaTime;
            }

            if (tempsDecayExterne <= 0f)
            {
                vitesseExterne = Vector3.zero;
                sortieDeBigAir = false;
            }
        }
    }

    private void AppliquerVitesseExterne(Vector3 vitesse, float duree = 0.5f)
    {
        vitesseExterne = vitesse;
        tempsDecayExterne = duree;
    }

    private Vector3 CalculerVitesseSaut(Vector3 depart, Vector3 arrivee, float hauteurTrajectoire)
    {
        float gravity = Physics.gravity.y;
        float displacementY = arrivee.y - depart.y;
        Vector3 displacementXZ = new Vector3(arrivee.x - depart.x, 0f, arrivee.z - depart.z);
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * hauteurTrajectoire);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * hauteurTrajectoire / gravity) + Mathf.Sqrt(2 * (displacementY - hauteurTrajectoire) / gravity));

        return velocityXZ + velocityY;
    }

    private void InitialiserGrappin(Vector3 cible, ModeGrappin modeGrappin)
    {
        actif = true;
        pointGrapple = cible;
        mode = modeGrappin;
        graviteReduite = false;
        timerGraviteReduite = 0f;
        timerSecuriteSaut = 0f;
    }

    private void ReinitialiserEtat()
    {
        actif = false;
        vitesseSaut = Vector3.zero;
        vitesseAjoutee = Vector3.zero;
        timerSecuriteSaut = 0f;
        graviteReduite = false;
        timerGraviteReduite = 0f;
        sortieDeBigAir = false;
    }
}