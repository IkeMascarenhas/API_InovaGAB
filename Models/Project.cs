using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InovaGAB.API.Models;

public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [BsonElement("ideiaId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string IdeiaId { get; set; } = string.Empty;

    [BsonElement("strategyId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StrategyId { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "planejamento";

    [BsonElement("gestorId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string GestorId { get; set; } = string.Empty;

    [BsonElement("investimento")]
    public double Investimento { get; set; } = 0.0;

    [BsonElement("retornoEsperado")]
    public double RetornoEsperado { get; set; } = 0.0;

    [BsonElement("prazoMeses")]
    public int PrazoMeses { get; set; } = 0;

    [BsonElement("progresso")]
    public int Progresso { get; set; } = 0;

    [BsonIgnore]
    public double RoiPercentual =>
        Investimento > 0 ? ((RetornoEsperado - Investimento) / Investimento) * 100.0 : 0.0;

    [BsonElement("dataCriacao")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
