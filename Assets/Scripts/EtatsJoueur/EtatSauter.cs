using UnityEngine;

namespace EtatsJoueur
{
    // Le joueur est en saut et attend qu'une action se produise
    public class EtatSauter : EtatJoueur
    {
        // Actions a accomplir lorsqu'un joueur est dans cet etat
        public override void EntrerEtat(Joueur joueur)
        {
            base.EntrerEtat(joueur);
            joueur.Sauter();
        }

        // Attend de recevoir un signal
        protected override EtatJoueur ExecuterEtatSpecifique(Joueur joueur)
        {
            if (joueur.ControleurCharacter.isGrounded)
            {
                if (joueur.EnMouvement)
                {
                    return new EtatDeplacement();
                }

                else
                {
                    return new EtatAttente();
                }
            }

            if (joueur.EnGrappin)
            {
                return new EtatGrappin();
            }

            return this;
        }
    }
}