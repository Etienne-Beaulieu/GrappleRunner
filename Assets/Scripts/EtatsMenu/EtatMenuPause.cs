using UnityEngine;

namespace EtatsMenu
{
    // Le menu est en mode pause et attend qu'une action se produise
    public class EtatMenuPause : EtatMenu
    {
        // Actions a accomplir lorsqu'un menu est dans cet etat
        public override void EntrerEtat(ControleurJeu controleur)
        {
            base.EntrerEtat(controleur);

            Time.timeScale = 0f;

            controleur.DesactiverTousLesMenus();

            if (controleur.MenuPause != null)
            {
                controleur.MenuPause.SetActive(true);
            }

            if (controleur.Chronometre != null)
            {
                controleur.Chronometre.MettrePause();
            }
        }

        // Attend de recevoir un signal
        public override EtatMenu ExecuterEtat(ControleurJeu controleur)
        {
            if (controleur.DemandeReprise)
            {
                Time.timeScale = 1f;

                controleur.DemandeReprise = false;
                return new EtatEnJeu();
            }

            return this;
        }

        // Actions a accomplir lorsqu'un menu sort de cet etat
        public override void SortirEtat(ControleurJeu controleur)
        {
            base.SortirEtat(controleur);

            Time.timeScale = 1f;
        }
    }
}