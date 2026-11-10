using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    [Header("Puntaje Sincronizado")]
    public NetworkVariable<int> score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void ModifyScore(int amount)
    {
        if (IsServer)
        {
            score.Value += amount;
            Debug.Log($"Puntaje actualizado para {gameObject.name}: {score.Value}");
        }
    }
}