using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Notebook/Encyclopedia")]
public class ResearchEncyclopedia : ScriptableObject
{
    // Public accessors
    public Dictionary<ResearchEncyclopediaArticleID, ResearchEncyclopediaArticle> Articles => articles;

    // Private editor data
    [SerializeField]
    [Tooltip("List of all the articles in this encyclopedia")]
    private List<ResearchEncyclopediaArticle> articlesList;

    // Private data
    [Tooltip("Maps the id of the articles to the article itself for faster lookup")]
    private Dictionary<ResearchEncyclopediaArticleID, ResearchEncyclopediaArticle> articles = new Dictionary<ResearchEncyclopediaArticleID, ResearchEncyclopediaArticle>();

    public void Setup()
    {
        foreach(ResearchEncyclopediaArticle article in articlesList)
        {
            article.Setup();
            if (!articles.ContainsKey(article.ID)) articles.Add(article.ID, article);
        }
    }
    // Get the article with the given ID
    public ResearchEncyclopediaArticle GetArticle(ResearchEncyclopediaArticleID id) => Articles[id];
}
