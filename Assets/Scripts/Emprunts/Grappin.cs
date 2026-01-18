using UnityEngine;
using System.Collections;

public class Grappin : MonoBehaviour
{
    // Dependances
    [Header("Dependances")]
    [Tooltip("Camera pour le raycast du grappin")]
    public Transform cam;
    [Tooltip("Point de depart visuel du grappin")]
    public Transform gunTip;
    [Tooltip("Couches sur lesquelles le grappin peut s'accrocher")]
    public LayerMask whatIsGrappleable;
    [Tooltip("Ligne de rendu pour visualiser la corde")]
    public LineRenderer lr;

    // Parametres
    [Header("Grappling")]
    [Tooltip("Distance maximale du grappin")]
    public float maxGrappleDistance = 30f;
    [Tooltip("Delai avant l'execution du saut")]
    public float grappleDelayTime = 0.15f;

    [Header("Cooldown")]
    [Tooltip("Temps de recharge entre deux utilisations")]
    public float grapplingCd = 0.75f;

    [Header("Parametres Saut")]
    [Tooltip("Seuil de hauteur pour utiliser saut direct (en metres)")]
    [SerializeField] private float seuilHauteurSautDirect = 4f;
    [Tooltip("Vitesse de base pour les sauts directs vers le haut")]
    [SerializeField] private float vitesseSautDirectBase = 22f;
    [Tooltip("Multiplicateur de vitesse pour les sauts horizontaux")]
    [SerializeField] private float multiplicateurSautHorizontal = 1.5f;
    [Tooltip("Délai de détachement minimum")]
    [SerializeField] private float delaiDetachementMin = 0.6f;
    [Tooltip("Délai de détachement maximum")]
    [SerializeField] private float delaiDetachementMax = 1.2f;

    // Effets
    [Header("Effets")]
    [Tooltip("Son joue lors du lancer du grappin")]
    [SerializeField] private AudioClip sonGrappin;

    [Header("Animation Corde Physique")]
    [SerializeField] private float graviteCorde = 0.3f;
    [SerializeField] private float tensionCorde = 1.2f;
    [SerializeField] private float dureeDeploiement = 0.5f;
    [SerializeField] private float dureeStabilisation = 0.7f;

    // Dependances
    private Joueur joueur;
    private ControleurInterface controleurInterface;
    private AudioSource sourceAudio;

    // Privés
    private bool grappling;
    private Vector3 grapplePoint;
    private float grapplingCdTimer;

    // GETTERS / SETTERS
    public LayerMask MaskGrappable => whatIsGrappleable;
    public float DistanceMax => maxGrappleDistance;
    public Vector3 GrapplePoint { get => grapplePoint; set => grapplePoint = value; }

    // === UNITY ===

    private void Awake()
    {
        joueur = GetComponent<Joueur>();

        // J'ajoute ma source audio pour le son
        sourceAudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        cam ??= joueur.TransformCamera;
        gunTip ??= joueur.BoutGrappin;

        // J'ai besoin de l'interface pour le cooldown
        controleurInterface = ControleurJeu.Instance?.ControleurInterface;
    }

    private void Update()
    {
        if (grapplingCdTimer > 0)
        {
            grapplingCdTimer -= Time.deltaTime;
        }

        // Je met a jour l'interface de recharge
        MettreAJourUIRecharge();
    }

    private void LateUpdate()
    {
        if (!grappling || lr == null || gunTip == null) return;
        lr.SetPosition(0, gunTip.position);
    }

    // === FONCTIONS ===

    public void TryGrapple(Vector3 rayOrigin, Vector3 rayDirection, MouvementGrappin.ModeGrappin mode)
    {
        if (grapplingCdTimer > 0) return;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            grappling = true;

            DemarrerVisuels();

            // J'ai fais joue un son
            if (sonGrappin != null && sourceAudio != null)
            {
                sourceAudio.PlayOneShot(sonGrappin);
            }

            if (mode == MouvementGrappin.ModeGrappin.Balancer)
            {
                ExecuteGrapple(mode);
            }

            else
            {
                Invoke(nameof(ExecuteGrappleJump), grappleDelayTime);
            }
        }
    }

    public void ArreterGrappin()
    {
        if (!grappling) return;

        grappling = false;
        StopAllCoroutines();
        joueur.mouvementGrappin.ArreterGrappin();
        grapplingCdTimer = grapplingCd;

        if (lr != null)
        {
            lr.enabled = false;
        }
    }

    private void ExecuteGrappleJump() => ExecuteGrapple(MouvementGrappin.ModeGrappin.SauterVersPoint);

    private void ExecuteGrapple(MouvementGrappin.ModeGrappin modeGrappin)
    {
        if (modeGrappin == MouvementGrappin.ModeGrappin.SauterVersPoint)
        {
            // Caller ma fonction que j'ai trouvee pour regler mon bug
            joueur.AnnulerToutMouvement();

            Vector3 deplacement = grapplePoint - transform.position;
            float distanceTotale = deplacement.magnitude;

            if (distanceTotale < 0.5f)
            {
                Debug.LogWarning("Cible trop proche, grappin annulé");
                ArreterGrappin();
                return;
            }

            float differenceHauteur = deplacement.y;

            if (differenceHauteur > seuilHauteurSautDirect)
            {
                CalculerSautDirect(deplacement, distanceTotale, differenceHauteur);
            }

            else
            {
                float distanceHorizontale = new Vector3(deplacement.x, 0f, deplacement.z).magnitude;
                CalculerSautParabolique(deplacement, distanceHorizontale, differenceHauteur);
            }

            float delaiDetachement = Mathf.Lerp(delaiDetachementMin, delaiDetachementMax, Mathf.Clamp01(distanceTotale / maxGrappleDistance));
            Invoke(nameof(DetacherVisuel), delaiDetachement);
        }

        else if (modeGrappin == MouvementGrappin.ModeGrappin.Balancer)
        {
            joueur.mouvementGrappin.DemarrerBalancer(grapplePoint);
        }
    }

    private void CalculerSautDirect(Vector3 deplacement, float distanceTotale, float differenceHauteur)
    {
        float tempsVise = Mathf.Clamp(Mathf.Lerp(0.6f, 1.2f, distanceTotale / maxGrappleDistance), 0.3f, 1.2f);
        float gravity = Mathf.Abs(Physics.gravity.y);
        float vitesseVerticale = Mathf.Max((differenceHauteur / tempsVise) + (0.5f * gravity * tempsVise), vitesseSautDirectBase);

        Vector3 directionHorizontale = new Vector3(deplacement.x, 0f, deplacement.z);
        float distanceHorizontale = directionHorizontale.magnitude;
        Vector3 vitesseHorizontale = distanceHorizontale > 0.1f ? (directionHorizontale / distanceHorizontale) * (distanceHorizontale / tempsVise) : Vector3.zero;

        Vector3 vitesseFinale = vitesseHorizontale + Vector3.up * vitesseVerticale;

        if (float.IsNaN(vitesseFinale.x) || float.IsNaN(vitesseFinale.y) || float.IsNaN(vitesseFinale.z))
        {
            Debug.LogError("NaN détecté dans CalculerSautDirect!");
            vitesseFinale = deplacement.normalized * vitesseSautDirectBase;
        }

        joueur.mouvementGrappin.AppliquerSaut(vitesseFinale);
    }

    private void CalculerSautParabolique(Vector3 deplacement, float distanceHorizontale, float differenceHauteur)
    {
        float arcBase = Mathf.Max(distanceHorizontale * 0.35f, 1.5f);
        float hauteurArc = differenceHauteur < 0
            ? Mathf.Max(arcBase * 0.5f, 2.5f)
            : arcBase + Mathf.Max(differenceHauteur * 0.25f, 0f);

        hauteurArc = Mathf.Clamp(hauteurArc, 2.5f, distanceHorizontale * 0.7f + 4f);

        if (float.IsNaN(hauteurArc))
        {
            Debug.LogError("NaN détecté dans CalculerSautParabolique!");
            hauteurArc = 5f;
        }

        joueur.mouvementGrappin.SauterParabolique(grapplePoint, hauteurArc, multiplicateurSautHorizontal);
    }

    private void DetacherVisuel()
    {
        if (!grappling) return;

        grappling = false;
        StopAllCoroutines();

        if (lr != null)
        {
            lr.enabled = false;
        }

        grapplingCdTimer = grapplingCd;
    }

    private void DemarrerVisuels()
    {
        if (lr != null && gunTip != null)
        {
            lr.enabled = true;
            lr.SetPosition(1, grapplePoint);
            StartCoroutine(AnimerCordePhysique());
        }
    }

    private IEnumerator AnimerCordePhysique()
    {
        int nombreSegments = 20;
        if (lr != null)
        {
            lr.positionCount = nombreSegments + 1;
        }

        bool snapTermine = false;
        float tempsDeploiement = 0f;
        float tempsApresSnap = 0f;

        while (grappling && lr != null && gunTip != null)
        {
            tempsDeploiement += Time.deltaTime;

            Vector3 debut = gunTip.position;
            Vector3 fin = grapplePoint;

            float progression = Mathf.Clamp01(tempsDeploiement / dureeDeploiement);

            if (progression >= 1f && !snapTermine)
            {
                tempsApresSnap += Time.deltaTime;
                if (tempsApresSnap >= dureeStabilisation)
                {
                    snapTermine = true;
                }
            }

            lr.SetPosition(0, debut);

            for (int i = 1; i < nombreSegments; i++)
            {
                float ratio = i / (float)nombreSegments;
                Vector3 pointBase = Vector3.Lerp(debut, fin, ratio);
                float offsetY = 0f;

                if (!snapTermine)
                {
                    if (progression < 1f)
                    {
                        float distanceDuFront = Mathf.Abs(ratio - progression);
                        float amplitude = (1f - ratio) * tensionCorde * 0.5f;

                        if (distanceDuFront < 0.3f)
                        {
                            float vague = Mathf.Sin(ratio * Mathf.PI * 4f - progression * Mathf.PI * 4f);
                            offsetY = vague * amplitude * (1f - distanceDuFront / 0.3f);
                        }
                    }
                    else
                    {
                        float facteurStabilisation = 1f - (tempsApresSnap / dureeStabilisation);
                        float rebond = Mathf.Sin(tempsApresSnap * 10f);
                        float amplitude = (1f - ratio) * tensionCorde * 0.3f;
                        offsetY = rebond * amplitude * facteurStabilisation;
                    }

                    offsetY -= graviteCorde * Mathf.Sin(ratio * Mathf.PI) * 0.15f;
                }

                pointBase.y += offsetY;
                lr.SetPosition(i, pointBase);
            }

            lr.SetPosition(nombreSegments, fin);

            yield return null;
        }
    }

    // Ma fonction pour envoyer le pourcentage calcule a la barre de l'interface
    private void MettreAJourUIRecharge()
    {
        if (controleurInterface == null) return;

        float pourcentage = (grapplingCdTimer > 0) ? 1f - (grapplingCdTimer / grapplingCd) : 1f;
        controleurInterface.UpdateCooldownGrappin(pourcentage);
    }
}