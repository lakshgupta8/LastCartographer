using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Something Wren can use by pressing up while standing in its trigger (the Hollow Knight
    /// convention; no dedicated interact button). Subclass in its own file and override Interact.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] string _prompt = "Look";
        public string Prompt => _prompt;

        /// <summary>False while dialogue or a cutscene should block interaction.</summary>
        public virtual bool CanInteract(Interactor who) => isActiveAndEnabled;

        public abstract void Interact(Interactor who);

        protected virtual void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
    }
}
