using UnityEngine;

namespace EtatsJoueur
{
    // Gere le comportement d'un joueur
    public abstract class EtatJoueur
    {
        // Actions a accomplir lorsqu'un joueur entre dans l'etat
        public virtual void EntrerEtat(Joueur joueur)
        {
            Debug.Log($"Joueur entre dans l'etat : {this.GetType().Name}");
        }

        // Actions a accomplir lorsqu'un joueur est dans cet etat. Comprends aussi la logique de passage aux autres etats.
        public virtual EtatJoueur ExecuterEtat(Joueur joueur)
        {
            if (joueur.Meurt) return new EtatMort();

            if (joueur.transform.position.y < joueur.hauteurMort)
            {
                joueur.Mourir();
                return new EtatMort();
            }

            return ExecuterEtatSpecifique(joueur);
        }

        protected abstract EtatJoueur ExecuterEtatSpecifique(Joueur joueur);

        // Actions a accomplir lorsqu'un joueur sort de l'etat
        public virtual void SortirEtat(Joueur joueur)
        {
            Debug.Log($"Joueur sort de l'etat : {this.GetType().Name}");
        }
    }
}