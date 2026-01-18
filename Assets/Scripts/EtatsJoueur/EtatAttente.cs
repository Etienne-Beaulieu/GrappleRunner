using UnityEngine;

namespace EtatsJoueur
{
    // Le joueur est immobile et attend qu'une action se produise
    public class EtatAttente : EtatJoueur
    {
        // Actions a accomplir lorsqu'un joueur est dans cet etat
        public override void EntrerEtat(Joueur joueur)
        {
            base.EntrerEtat(joueur);
        }

        // Attend de recevoir un signal
        protected override EtatJoueur ExecuterEtatSpecifique(Joueur joueur)
        {
            if (joueur.EnMouvement)
            {
                return new EtatDeplacement();
            }

            if (joueur.EnSaut && joueur.ControleurCharacter.isGrounded)
            {
                joueur.EnSaut = false;
                return new EtatSauter();
            }

            if (joueur.EnGrappin)
            {
                return new EtatGrappin();
            }

            return this;
        }
    }
}