using UnityEngine;

namespace EtatsJoueur
{
    // Le joueur est en grappin et attend qu'une action se produise
    public class EtatGrappin : EtatJoueur
    {
        // Actions a accomplir lorsqu'un joueur est dans cet etat
        public override void EntrerEtat(Joueur joueur)
        {
            base.EntrerEtat(joueur);
        }

        // Attend de recevoir un signal
        protected override EtatJoueur ExecuterEtatSpecifique(Joueur joueur)
        {
            if (!joueur.EnGrappin)
            {
                if (joueur.EnMouvement)
                {
                    return new EtatDeplacement();
                }

                return new EtatAttente();
            }

            return this;
        }
    }
}