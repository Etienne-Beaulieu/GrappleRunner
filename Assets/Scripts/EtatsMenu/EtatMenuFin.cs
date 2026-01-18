using UnityEngine;

namespace EtatsMenu
{
    // Le menu est en mode fin et attend qu'une action se produise
    public class EtatMenuFin : EtatMenu
    {
        // Actions a accomplir lorsqu'un menu est dans cet etat
        public override void EntrerEtat(ControleurJeu controleur)
        {
            base.EntrerEtat(controleur);

            controleur.DesactiverTousLesMenus();

            if (controleur.MenuFinPartie != null)
            {
                controleur.MenuFinPartie.SetActive(true);
            }
        }

        // Attend de recevoir un signal
        public override EtatMenu ExecuterEtat(ControleurJeu controleur)
        {
            return this;
        }
    }
}