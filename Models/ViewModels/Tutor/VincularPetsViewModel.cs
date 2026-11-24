namespace VetCare.Models.ViewModels.Tutor;

public record VincularPetsViewModel(int TutorId, string TutorNome, IReadOnlyList<PetDisponivelViewModel> Pets,
    IReadOnlySet<int> Selecionados);

// O ID do tutor atual é mantido para revalidar o vínculo na confirmação da transferência.
public record PetDisponivelViewModel(int Id, string Nome, int TutorAtualId, string Raca, string TutorAtual);
