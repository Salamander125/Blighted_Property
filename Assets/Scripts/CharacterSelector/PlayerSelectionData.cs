using System.Collections.Generic;

public static class PlayerSelectionData
{
    // Para partidas nuevas
    public static List<CharacterDataSO> ChosenCharacters = new List<CharacterDataSO>();

    // Para cargar partidas existentes (NUEVO)
    public static MatchRequest PartidaCargada = null;
}