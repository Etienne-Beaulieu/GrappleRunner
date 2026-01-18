using UnityEngine;

namespace EtatsMenu
{
    // Le menu est en mode principal et attend qu'une action se produise
    public class EtatMenuPrincipal : EtatMenu
    {
        // Actions a accomplir lorsqu'un menu est dans cet etat
        public override void EntrerEtat(ControleurJeu controleur)
        {
            base.EntrerEtat(controleur);

            controleur.DesactiverTousLesMenus();

            if (controleur.MenuPrincipal != null)
            {
                controleur.MenuPrincipal.SetActive(true);
            }
        }

        // Attend de recevoir un signal
        public override EtatMenu ExecuterEtat(ControleurJeu controleur)
        {
            if (controleur.DemandeDebut)
            {
                controleur.DemandeDebut = false;
                return new EtatEnJeu();
            }

            return this;
        }
    }
}