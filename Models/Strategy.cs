using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InovaGAB.API.Models;

/// <summary>
/// Estratégia Corporativa — gerenciada exclusivamente por Líderes.
/// Vincula Ideias e Projetos a um eixo estratégico da empresa.
/// </summary>
public class Strategy
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [BsonElement("descricao")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da estratégia. Ex: Eficiência Operacional, Sustentabilidade, 
    /// Redução de Custos, Experiência do Colaborador.
    /// </summary>
    [BsonElement("categoria")]
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Nome da campanha de inovação associada (ex: "Inova Verão 2025").</summary>
    [BsonElement("campanha")]
    public string Campanha { get; set; } = string.Empty;

    /// <summary>Status: ativo | inativo</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "ativo";

    [BsonElement("dataCriacao")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
