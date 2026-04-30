using UnityEngine;

public class CharacterAnimController : StateMachineBehaviour
{
    [Header("Configuración de Tiempo (Vueltas de la animación)")]
    [Tooltip("Cuántas veces se reproduce el Idle Normal antes de cambiar el pie")]
    public int vueltasParaAlterar = 4;
    [Tooltip("Cuántas veces se reproduce el Idle Alterado antes de volver a normal")]
    public int vueltasParaDesalterar = 3;

    [Header("Probabilidad de Parpadeo")]
    [Range(0f, 1f)]
    public float probabilidadParpadeo = 0.4f; // 40% de chance al final de cada vuelta

    [Header("Triggers")]
    public string triggerParpadeo = "t_parpadeo";
    public string triggerParpadeoAlterado = "t_parpadeoAlterado";
    public string triggerAlterar = "t_alterar";
    public string triggerDesalterar = "t_desalterar";

    // Variables internas
    private int vueltasCompletadas = 0;
    private int ultimoCicloEntero = 0;
    private bool evaluadoEnEstaVuelta = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ultimoCicloEntero = 0;
        evaluadoEnEstaVuelta = false;

        // IMPORTANTE: NO reiniciamos las vueltasCompletadas aquí.
        // Si parpadea, queremos que recuerde cuántas vueltas llevaba al volver.

        animator.ResetTrigger(triggerParpadeo);
        animator.ResetTrigger(triggerParpadeoAlterado);
        animator.ResetTrigger(triggerAlterar);
        animator.ResetTrigger(triggerDesalterar);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int cicloActual = Mathf.FloorToInt(stateInfo.normalizedTime);
        float fraccionTiempo = stateInfo.normalizedTime - cicloActual;

        // Al empezar una nueva vuelta, permitimos volver a tomar decisiones
        if (cicloActual > ultimoCicloEntero)
        {
            ultimoCicloEntero = cicloActual;
            evaluadoEnEstaVuelta = false;
        }

        // Evaluamos al final de la vuelta (85%)
        if (fraccionTiempo > 0.85f && !evaluadoEnEstaVuelta)
        {
            vueltasCompletadas++; // Sumamos una vuelta a la cuenta general

            if (stateInfo.IsTag("IdleNormal"))
            {
                // 1. Comprobamos si ya toca cambiar de postura
                if (vueltasCompletadas >= vueltasParaAlterar)
                {
                    animator.SetTrigger(triggerAlterar);
                    vueltasCompletadas = 0; // Reseteamos la cuenta AL CAMBIAR de postura
                }
                // 2. Si no toca cambiar, tiramos los dados para el parpadeo
                else if (Random.value <= probabilidadParpadeo)
                {
                    animator.SetTrigger(triggerParpadeo);
                }
            }
            else if (stateInfo.IsTag("IdleAlterado"))
            {
                if (vueltasCompletadas >= vueltasParaDesalterar)
                {
                    animator.SetTrigger(triggerDesalterar);
                    vueltasCompletadas = 0; // Reseteamos la cuenta AL CAMBIAR de postura
                }
                else if (Random.value <= probabilidadParpadeo)
                {
                    animator.SetTrigger(triggerParpadeoAlterado);
                }
            }

            evaluadoEnEstaVuelta = true; // Evitamos que evalúe 50 veces por segundo
        }
    }
}