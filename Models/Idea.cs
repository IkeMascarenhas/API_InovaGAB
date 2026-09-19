using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InovaGAB.API.Models;

public class Idea
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [BsonElement("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "pendente";

    [BsonElement("autorId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AutorId { get; set; } = string.Empty;

    [BsonElement("autorNome")]
    public string AutorNome { get; set; } = string.Empty;

    [BsonElement("strategyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StrategyId { get; set; } = string.Empty;

    [BsonElement("comentarioGestor")]
    public string ComentarioGestor { get; set; } = string.Empty;

    [BsonElement("pontuacaoIA")]
    public int PontuacaoIA { get; set; } = 0;

    [BsonElement("justificativaIA")]
    public string JustificativaIA { get; set; } = string.Empty;

    [BsonElement("dataCriacao")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
