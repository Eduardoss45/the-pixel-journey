# Quiz

- QuizManager
  > Deve apontar para o arquivo onde estão listadas a quest e repassar o controle para o quizBlock, este arquivo é carregado automaticamente na inicialização
- QuizBlock
  > Abrir o quiz ao colidir
  > Não pode háver conflitos entre outros QuizBlocks (mais de um)
  > Controlar o conteúdo exibido no quiz de forma modular
  > Podemos passar um array de ids (quiz multi-etapas) ou apenas um, esses dados vem do QuizManager
- QuizUI
  > Exibir label para a pergunta
  > Exibir 4 opções com escolha única
  > Feedback visual ao verificar a resposta e em caso de erro repetir a mesma pergunta novamente até que seja acertada e por fim finalizar o quiz
  > Não tem botão de sair, e é obrigatório
  > Tem um contador que diz a quantidade de etapas que o quiz possui