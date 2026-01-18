using UnityEngine;

namespace EtatsMenu
{
    // Gere le comportement d'un menu
    public abstract class EtatMenu
    {
        // Actions a accomplir lorsqu'un menu entre dans l'etat
        public virtual void EntrerEtat(ControleurJeu controleur)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            controleur.joueur.ControleActif = false;

            Debug.Log($"Menu entre dans l'etat : {GetType().Name}");
        }

        // Actions a accomplir lorsqu'un menu est dans cet etat. Comprends aussi la logique de passage aux autres etats.
        public abstract EtatMenu ExecuterEtat(ControleurJeu controleur);

        // Actions a accomplir lorsqu'un menu sort de l'etat
        public virtual void SortirEtat(ControleurJeu controleur)
        {
            Debug.Log($"Menu sort de l'etat : {GetType().Name}");
        }
    }
}