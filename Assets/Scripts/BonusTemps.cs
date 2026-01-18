using UnityEngine;
using System;

// Classe qui gere mon bonus de temps
public class BonusTemps : MonoBehaviour
{
    // Effets
    [Header("Effets")]
    [SerializeField] private AudioClip sonCollecte;

    // Parametres
    [Header("Parametres")]
    [SerializeField] private float tempsAjoute = 15f;
    [SerializeField] private float delaiReapparition = 30f;

    // Composantes de l'objet
    private Vector3 positionInitiale;
    private MeshRenderer rendu;
    private new Collider collider;
    private AudioSource sourceAudio;

    // === UNITY ===

    /// <summary>
    /// Get les composantes au depart
    /// </summary>
    void Start()
    {
        positionInitiale = transform.position;
        rendu = GetComponentInChildren<MeshRenderer>();
        collider = GetComponent<Collider>();
        sourceAudio = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Sur collision avec le joueur on lance collecter
    /// </summary>
    /// <param name="autre"></param>
    void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            Collecter();
        }
    }

    // === FONCTIONS ===

    /// <summary>
    /// Fonction pour gerer la collecte du bonus
    /// </summary>
    void Collecter()
    {
        // Get le chronometre et ajouter le temps
        Chronometre.Instance?.AjouterTemps(tempsAjoute);

        // Jouer le son
        if (sonCollecte != null && sourceAudio != null)
        {
            sourceAudio.PlayOneShot(sonCollecte);
        }

        // Desactiver l'objet
        rendu.enabled = false;
        collider.enabled = false;

        // Partir la coroutine de respawn
        StartCoroutine(Reapparaitre());
    }

    /// <summary>
    /// Coroutine pour faire reapparaitre le bonus
    /// </summary>
    /// <returns></returns>
    System.Collections.IEnumerator Reapparaitre()
    {
        // On attend le delai choisi
        yield return new WaitForSeconds(delaiReapparition);

        // Reactiver l'objet
        rendu.enabled = true;
        collider.enabled = true;
        transform.position = positionInitiale;
    }
}