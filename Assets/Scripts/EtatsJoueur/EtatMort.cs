using UnityEngine;

namespace EtatsJoueur
{
    // Le joueur est mort et attend qu'une action se produise
    public class EtatMort : EtatJoueur
    {
        // Actions a accomplir lorsqu'un joueur est dans cet etat
        public override void EntrerEtat(Joueur joueur)
        {
            base.EntrerEtat(joueur);
        }

        // Attend de recevoir un signal
        protected override EtatJoueur ExecuterEtatSpecifique(Joueur joueur)
        {
            joueur.EnSaut = false;

            if (!joueur.Meurt && joueur.ControleurCharacter.isGrounded)
            {
                return new EtatAttente();
            }

            return this;
        }
    }
}