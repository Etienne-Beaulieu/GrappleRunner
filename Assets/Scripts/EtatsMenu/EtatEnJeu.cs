using UnityEngine;

namespace EtatsMenu
{
    // Le menu est en mode jeu et attend qu'une action se produise
    public class EtatEnJeu : EtatMenu
    {
        // Actions a accomplir lorsqu'un menu est dans cet etat
        public override void EntrerEtat(ControleurJeu controleur)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            controleur.joueur.ControleActif = true;

            controleur.DesactiverTousLesMenus();

            if (controleur.InterfaceJeu != null)
            {
                controleur.InterfaceJeu.SetActive(true);
            }

            if (controleur.Chronometre != null)
            {
                controleur.Chronometre.Reprendre();
            }
        }

        // Attend de recevoir un signal
        public override EtatMenu ExecuterEtat(ControleurJeu controleur)
        {
            if (controleur.DemandePause)
            {
                controleur.DemandePause = false;
                return new EtatMenuPause();
            }

            if (controleur.DemandeFin)
            {
                controleur.DemandeFin = false;
                return new EtatMenuFin();
            }

            return this;
        }
    }
}