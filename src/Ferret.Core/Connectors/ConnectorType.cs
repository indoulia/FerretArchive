namespace Ferret.Core.Connectors;

/// <summary>Identifies the category of a context source connector.</summary>
public enum ConnectorType
{
    /// <summary>Local file system.</summary>
    Filesystem = 0,

    /// <summary>Git version control.</summary>
    Git = 1,

    /// <summary>Atlassian JIRA.</summary>
    Jira = 2,

    /// <summary>GitHub.</summary>
    GitHub = 3,

    /// <summary>Azure DevOps.</summary>
    AzureDevOps = 4,

    /// <summary>Atlassian Confluence.</summary>
    Confluence = 5,

    /// <summary>Microsoft SharePoint.</summary>
    SharePoint = 6,

    /// <summary>Slack messaging.</summary>
    Slack = 7,

    /// <summary>Microsoft Teams.</summary>
    Teams = 8,

    /// <summary>Log files and streams.</summary>
    Logs = 9,

    /// <summary>User-defined connector.</summary>
    Custom = 99,
}
