A cena alvo é tropic.tscn pois esta previamente configurada (prototipo)

Player perde vida
Tela escurece
Animação toca
HUD mostra vidas restantes
Se vidas > 0 → reinicia fase
Se vidas = 0 → Game Over scene
Não troca cena imediatamente.
Usar GameState Machine:

public enum GameState
{
    Playing,
    PlayerDead,
    GameOver
}

Aplicar número de vidas no CountLifes
O jogo sempre inicia com 5 vidas
O Player devera ativar a mecânica de morte se estiver com o eixo y maior que 300px
Morrer devera resetar o jogo globalmente para evitar problemas de estado