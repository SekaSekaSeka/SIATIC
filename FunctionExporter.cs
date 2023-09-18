using Amazon.Lambda.Core;
using Npgsql;
using System.Collections;
using System.Data;
using Amazon.S3;
using System.Text;
using Amazon.S3.Model;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Xml;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LimiteStockageExporter;

#region Déclaration des VO
public enum TypeExtension
{
    /// <summary>
    /// Xml
    /// </summary>
    Xml,

    /// <summary>
    /// Csv
    /// </summary>
    Csv,

    /// <summary>
    ///  Par defaut
    /// </summary>
    Defaut
}

public sealed class TypeExtensionParser
{
    /// <summary>
    /// Try Parse.
    /// </summary>
    /// <param name="valeur">Valeur</param>
    /// <param name="typeExtension">Type d'extension</param>
    public static void TryParse(string valeur, out TypeExtension typeExtension)
    {
        typeExtension = !Enum.IsDefined(typeof(TypeExtension), UpperFirst(valeur))
            ? TypeExtension.Defaut
            : (TypeExtension)Enum.Parse(typeof(TypeExtension), UpperFirst(valeur));
    }


    private static string UpperFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return char.ToUpper(text[0]) +
            ((text.Length > 1) ? text.Substring(1).ToLower() : string.Empty);
    }

}

public class VORequestPublierSequence
{
    /// <summary>
    /// Sequence
    /// </summary>
    public VOSequence VoSequence { get; set; }

    /// <summary>
    /// Clé (un ou plusieurs objets concaténées)
    /// </summary>
    /// <param name="clesConcatenees">
    /// La clé.
    /// Exemple pour la clé "Cle1aaaaCle1bbbb" :
    ///     paramètres -> "Cle1aaaa","Cle1bbbb"
    /// </param>
    public void SetCle(params object[] clesConcatenees)
    {
        if (this.VoSequence == null)
        {
            this.VoSequence = new VOSequence();
        }
        this.VoSequence.Cle = Concatenees(clesConcatenees);
    }

    /// <summary>
    /// Concaténation de un à plusieurs objets
    /// </summary>
    /// <param name="concatenees">
    /// Objets à concatener
    /// </param>
    /// <returns>
    /// Un ou plusieurs objets concaténés
    /// </returns>
    private static string Concatenees(params object[] concatenees)
    {
        var concateneesString = new StringBuilder();
        foreach (var objet in concatenees)
        {
            concateneesString.Append(objet.ToString());
        }

        return concateneesString.ToString();
    }
}

public class VOSequence
{
    #region Properties

    /// <summary>
    /// Clé
    /// </summary>
    public virtual string Cle { get; set; }

    /// <summary>
    /// Valeur
    /// </summary>
    public virtual int Valeur { get; set; }


    /// <summary>
    /// Identifiant
    /// </summary>
    public virtual int Ident { get; set; }

    private bool _actif = true;

    /// <summary>
    /// Actif : O/N
    /// </summary>
    public virtual bool Actif
    {
        get { return _actif; }
        set { _actif = value; }
    }

    /// <summary>
    /// Auteur de mise à jour
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Date de mise à jour
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    #endregion

    public override string ToString()
    {
        return String.Format("{0:00000}", Valeur);
    }
}

public class VOExpediteurTransport
{
    /// <summary>
    /// Numéro compte auxiliaire
    /// </summary>
    private string ananumaux;

    /// <summary>
    /// Code APE
    /// </summary>
    private string apecod;

    /// <summary>
    /// Date de passage à la gestion conjointe active
    /// </summary>
    private DateTime dateeffetgc;

    /// <summary>
    /// Auteur dernière modification
    /// </summary>
    private string expautmaj;

    /// <summary>
    /// Chiffre d'affaire
    /// </summary>
    private int expcaf;

    /// <summary>
    /// Chiffre d'affaire transport
    /// </summary>
    private decimal expcafftr;

    /// <summary>
    /// Capacité transport
    /// </summary>
    private decimal expcapatr;

    /// <summary>
    /// Capital social
    /// </summary>
    private int expcapsoc;

    /// <summary>
    /// Divers
    /// </summary>
    private string expdiv;

    /// <summary>
    /// Date dernière modification
    /// </summary>
    private DateTime expdtemaj;

    /// <summary>
    /// Date début d'activité chez GSO
    /// </summary>
    private DateTime expdterac;

    /// <summary>
    /// Nombre de salariés
    /// </summary>
    private int expnbrsal;

    /// -------------------------------------------------------
    /// TABLE EXPEDITEUR
    /// -------------------------------------------------------
    /// <summary>
    /// Numéro expéditeur
    /// </summary>
    private int expnum;

    /// <summary>
    /// Numéro Identification
    /// </summary>
    private string expnumidt;

    /// <summary>
    /// Numéro de Siret
    /// </summary>
    private string expnumsir;

    /// <summary>
    /// Raison Sociale longue
    /// </summary>
    private string expraisoc;

    /// <summary>
    /// Raison Sociale courte
    /// </summary>
    private string exprsocrt;

    /// <summary>
    /// Statut social
    /// </summary>
    private string expstasoc;

    /// <summary>
    /// Commentaire Société mère
    /// </summary>
    private string expstmcom;

    /// <summary>
    /// Nom société mère
    /// </summary>
    private string expstmnom;

    /// <summary>
    /// Type Paiement TVA
    /// </summary>
    private int exptva;

    /// <summary>
    /// Indicateur de souscription à la Gestion Conjointe
    /// </summary>
    private int gc;

    /// <summary>
    /// Code NAF
    /// </summary>
    private string nafcod;

    /// <summary>
    /// Type expéditeur
    /// </summary>
    private string texcod;

    /// <summary>
    /// Transmis SBT
    /// </summary>
    private bool transmissbt;

    /// <summary>
    /// Initializes a new instance of the <see cref="VOExpediteurTransport"/> class. 
    /// </summary>
    public VOExpediteurTransport()
    {
        this.expnum = Int32.MinValue;
        this.expnumsir = String.Empty;
        this.texcod = String.Empty;
        this.apecod = String.Empty;
        this.nafcod = String.Empty;
        this.exprsocrt = String.Empty;
        this.expraisoc = String.Empty;
        this.expnbrsal = Int32.MinValue;
        this.expcaf = Int32.MinValue;
        this.expdterac = DateTime.MinValue;
        this.expstasoc = String.Empty;
        this.expcapsoc = Int32.MinValue;
        this.expdiv = String.Empty;
        this.ananumaux = String.Empty;
        this.expstmnom = String.Empty;
        this.expstmcom = String.Empty;
        this.expcapatr = 0;
        this.expcafftr = 0;
        this.expdtemaj = DateTime.MinValue;
        this.expautmaj = String.Empty;
        this.expnumidt = String.Empty;
        this.exptva = Int32.MinValue;
        this.dateeffetgc = DateTime.MinValue;
        this.gc = Int32.MinValue;
        this.transmissbt = false;
    }

    /// <summary>
    /// Accesseurs de l'attribut _expautmaj (Auteur dernière modification)
    /// </summary>
    /// <value>
    /// The auteur maj.
    /// </value>
    public virtual string AuteurMaj
    {
        get
        {
            return this.expautmaj;
        }

        set
        {
            this.expautmaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expcapatr (Capacité transport)
    /// </summary>
    /// <value>
    /// The capacite transport.
    /// </value>
    public virtual decimal CapaciteTransport
    {
        get
        {
            return this.expcapatr;
        }

        set
        {
            this.expcapatr = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expcapsoc (Capital social)
    /// </summary>
    /// <value>
    /// The capital social.
    /// </value>
    public virtual int CapitalSocial
    {
        get
        {
            return this.expcapsoc;
        }

        set
        {
            this.expcapsoc = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expcaf (Chiffre d'affaire)
    /// </summary>
    /// <value>
    /// The chiffre affaire.
    /// </value>
    public virtual int ChiffreAffaire
    {
        get
        {
            return this.expcaf;
        }

        set
        {
            this.expcaf = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expcafftr (Chiffre d'affaire transport)
    /// </summary>
    /// <value>
    /// The chiffre affaire transport.
    /// </value>
    public virtual decimal ChiffreAffaireTransport
    {
        get
        {
            return this.expcafftr;
        }

        set
        {
            this.expcafftr = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _apecod (Code APE)
    /// </summary>
    /// <value>
    /// The code ape.
    /// </value>
    public virtual string CodeAPE
    {
        get
        {
            return this.apecod;
        }

        set
        {
            this.apecod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _nafcod (Code NAF)
    /// </summary>
    /// <value>
    /// The code naf.
    /// </value>
    public virtual string CodeNAF
    {
        get
        {
            return this.nafcod;
        }

        set
        {
            this.nafcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expstmcom (Commentaire Société mère)
    /// </summary>
    /// <value>
    /// The commentaire societe mere.
    /// </value>
    public virtual string CommentaireSocieteMere
    {
        get
        {
            return this.expstmcom;
        }

        set
        {
            this.expstmcom = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expdterac (Date début d'activité chez GSO)
    /// </summary>
    /// <value>
    /// The date debut activite gso.
    /// </value>
    public virtual DateTime DateDebutActiviteGSO
    {
        get
        {
            return this.expdterac;
        }

        set
        {
            this.expdterac = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateeffetgc (Date de passage à la gestion conjointe active)
    /// </summary>
    /// <value>
    /// The date gestion conjointe active.
    /// </value>
    public virtual DateTime DateGestionConjointeActive
    {
        get
        {
            return this.dateeffetgc;
        }

        set
        {
            this.dateeffetgc = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expdtemaj (Date dernière modification)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get
        {
            return this.expdtemaj;
        }

        set
        {
            this.expdtemaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expdiv (Divers)
    /// </summary>
    /// <value>
    /// The divers.
    /// </value>
    public virtual string Divers
    {
        get
        {
            return this.expdiv;
        }

        set
        {
            this.expdiv = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _gc (Indicateur de souscription à la Gestion Conjointe)
    /// </summary>
    /// <value>
    /// The indicateur gestion conjointe.
    /// </value>
    public virtual int IndicateurGestionConjointe
    {
        get
        {
            return this.gc;
        }

        set
        {
            this.gc = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expnbrsal (Nombre de salariés)
    /// </summary>
    /// <value>
    /// The nb salaries.
    /// </value>
    public virtual int NbSalaries
    {
        get
        {
            return this.expnbrsal;
        }

        set
        {
            this.expnbrsal = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expnum (Numéro expéditeur)
    /// </summary>
    /// <value>
    /// The num.
    /// </value>
    public virtual int Num
    {
        get
        {
            return this.expnum;
        }

        set
        {
            this.expnum = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expnum pour les tris (Numéro expéditeur)
    /// </summary>
    /// <value>
    /// The num.
    /// </value>
    public virtual int NumSort
    {
        get
        {
            return this.expnum;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _ananumaux (Numéro compte auxiliaire)
    /// </summary>
    /// <value>
    /// The num compte auxiliaire.
    /// </value>
    public virtual string NumCompteAuxiliaire
    {
        get
        {
            return this.ananumaux;
        }

        set
        {
            this.ananumaux = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expnumidt (Numéro Identification)
    /// </summary>
    /// <value>
    /// The num identification.
    /// </value>
    public virtual string NumIdentification
    {
        get
        {
            return this.expnumidt;
        }

        set
        {
            this.expnumidt = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expnumsir (Numéro de Siret)
    /// </summary>
    /// <value>
    /// The num siret.
    /// </value>
    public virtual string NumSiret
    {
        get
        {
            return this.expnumsir;
        }

        set
        {
            this.expnumsir = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _exprsocrt (Raison Sociale courte)
    /// </summary>
    /// <value>
    /// The raison sociale court.
    /// </value>
    public virtual string RaisonSocialeCourt
    {
        get
        {
            return this.exprsocrt;
        }

        set
        {
            this.exprsocrt = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expraisoc (Raison Sociale longue)
    /// </summary>
    /// <value>
    /// The raison sociale long.
    /// </value>
    public virtual string RaisonSocialeLong
    {
        get
        {
            return this.expraisoc;
        }

        set
        {
            this.expraisoc = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expstmnom (Nom société mère)
    /// </summary>
    /// <value>
    /// The societe mere.
    /// </value>
    public virtual string SocieteMere
    {
        get
        {
            return this.expstmnom;
        }

        set
        {
            this.expstmnom = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _expstasoc (Statut social)
    /// </summary>
    /// <value>
    /// The statut social.
    /// </value>
    public virtual string StatutSocial
    {
        get
        {
            return this.expstasoc;
        }

        set
        {
            this.expstasoc = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _transmissbt (Transmis SBT)
    /// </summary>
    /// <value>
    /// The transmis sbt.
    /// </value>
    public virtual bool TransmisSBT
    {
        get
        {
            return this.transmissbt;
        }

        set
        {
            this.transmissbt = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _texcod (Type expéditeur)
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public virtual string Type
    {
        get
        {
            return this.texcod;
        }

        set
        {
            this.texcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _exptva (Type Paiement TVA)
    /// </summary>
    /// <value>
    /// The type paiement tva.
    /// </value>
    public virtual int TypePaiementTVA
    {
        get
        {
            return this.exptva;
        }

        set
        {
            this.exptva = value;
        }
    }

    /// <summary>
    /// Gets or sets ProprietesExpediteur.
    /// </summary>
    /// <value>
    /// The proprietes expéditeur.
    /// </value>
    public virtual IList ProprietesExpediteur { get; set; }

    /// <summary>
    /// Retourne le code EIC de l'expéditeur
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Code EIC
    /// </returns>
    //public virtual string GetCodeEic(DateTime date)
    //{
    //    return this.GetPropriete<string>("EIC", date);
    //}

    /// <summary>
    /// Récupération d'une propriété de point valide à une date donnée
    /// </summary>
    /// <typeparam name="T">
    /// Type de la valeur de propriété
    /// </typeparam>
    /// <param name="code">
    /// Code de la propriété
    /// </param>
    /// <param name="date">
    /// Date de référence
    /// </param>
    /// <returns>
    /// Valeur de propriété
    /// </returns>
    //private T GetPropriete<T>(string code, DateTime date)
    //{
    //    var result = default(T);
    //    var p = (from prop in this.ProprietesExpediteur.Cast<VOLienPropObj>()
    //             where prop.PropObj.Codeprop.Equals(code)
    //                && prop.Dateeff <= date
    //             orderby prop.Dateeff descending
    //             select prop).FirstOrDefault();
    //    if (p != null)
    //    {
    //        result = (T)Convert.ChangeType(p.Valeur, typeof(T));
    //    }

    //    return result;
    //}

    /// <summary>
    /// Convertit l'expéditeur en texte
    /// </summary>
    /// <returns>
    /// Expéditeur sous forme de chaîne de caractères
    /// </returns>
    public override string ToString()
    {
        return this.RaisonSocialeCourt;
    }

}

public class PKContrat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PKContrat"/> class.
    /// </summary>
    public PKContrat()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PKContrat"/> class.
    /// </summary>
    /// <param name="pNumContrat">
    /// The p num contrat.
    /// </param>
    /// <param name="pNumAvenant">
    /// The p num avenant.
    /// </param>
    public PKContrat(int pNumContrat, int pNumAvenant)
    {
        this.NumContrat = pNumContrat;
        this.NumAvenant = pNumAvenant;
    }



    /// <summary>
    /// Accesseurs de l'attribut _numAvenant (Numéro Avenant)
    /// </summary>
    /// <value>
    /// The num avenant.
    /// </value>
    public int NumAvenant
    {
        get;
        set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _numContrat (Numéro de contrat)
    /// </summary>
    /// <value>
    /// The num contrat.
    /// </value>
    public int NumContrat
    {
        get;
        set;
    }


    /// <summary>
    /// The equals.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// <returns>
    /// The equals.
    /// </returns>
    public override bool Equals(object obj)
    {
        var comparePk = obj as PKContrat;
        if (comparePk == null)
        {
            return false;
        }

        return this.NumAvenant == comparePk.NumAvenant && this.NumContrat == comparePk.NumContrat;
    }

    /// <summary>
    /// The get hash code.
    /// </summary>
    /// <returns>
    /// The get hash code.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = GetType().GetHashCode();
            hash = (hash * 31) ^ this.NumAvenant.GetHashCode();
            hash = (hash * 31) ^ this.NumContrat.GetHashCode();

            return hash;
        }
    }

    /// <summary>
    /// The to string.
    /// </summary>
    /// <returns>
    /// The to string.
    /// </returns>
    public override string ToString()
    {
        return string.Concat(this.NumAvenant, "-", this.NumContrat);
    }


}

public class VOContrat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VOContrat"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOContrat()
    {
        this.ExpediteurTransport = new VOExpediteurTransport();

        this.Type = String.Empty;
        this.CodeTarif = String.Empty;
        this.DateDebut = DateTime.MinValue;
        this.DateFin = DateTime.MinValue;
        this.DateSignature = DateTime.MinValue;
        this.DateEffetAvenant = DateTime.MinValue;
        this.PremierJourFacturation = Int32.MinValue;
        this.MoisContractuel = Int32.MinValue;
        this.ReconductionTacite = false;
        this.PreavisDenonciation = String.Empty;
        this.DeviseFacturation = String.Empty;
        this.CommentaireFinancement = String.Empty;
        this.ClausesParticulieres = String.Empty;
        this.Statut = false;
        this.MontantCaution = 0;
        this.DateExpirationCaution = DateTime.MinValue;
        this.EmetteurCaution = String.Empty;
        this.JourPaiement = Int32.MinValue;
        this.PaiementSiNonOuvre = false;
        this.SoumissionPenalites = false;
        this.ModePaiement = String.Empty;
        this.TypePaiement = String.Empty;
        this.DateDebutFlexibilite = DateTime.MinValue;
        this.DateFinFlexibilite = DateTime.MinValue;
        this.DateSignatureFlexibilite = DateTime.MinValue;
        this.DateDebutEquilibrage = DateTime.MinValue;
        this.DateFinEquilibrage = DateTime.MinValue;
        this.DateSignatureEquilibrage = DateTime.MinValue;
        this.DateDebutPEG = DateTime.MinValue;
        this.DateFinPEG = DateTime.MinValue;
        this.DateSignaturePEG = DateTime.MinValue;
        this.DateDebutPEGPowerNext = DateTime.MinValue;
        this.DateFinPEGPowerNext = DateTime.MinValue;
        this.DateSignaturePEGPowerNext = DateTime.MinValue;
        this.AutMaj = String.Empty;
        this.DateMaj = DateTime.MinValue;
    }


    /// <summary>
    /// Accesseurs de l'attribut _autMaj (Auteur dernière MAJ)
    /// </summary>
    /// <value>
    /// The aut maj.
    /// </value>
    public virtual string AutMaj
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _clausesParticulieres (Clauses particulières)
    /// </summary>
    /// <value>
    /// The clauses particulieres.
    /// </value>
    public virtual string ClausesParticulieres
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _codeTarif (Code tarif)
    /// </summary>
    /// <value>
    /// The code tarif.
    /// </value>
    public virtual string CodeTarif
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _commentaireFinancement (Commentaires financement)
    /// </summary>
    /// <value>
    /// The commentaire financement.
    /// </value>
    public virtual string CommentaireFinancement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateDebut (Date début du contrat)
    /// </summary>
    /// <value>
    /// The date debut.
    /// </value>
    public virtual DateTime DateDebut
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateDebutEquilibrage (Date début équilibrage)
    /// </summary>
    /// <value>
    /// The date debut equilibrage.
    /// </value>
    public virtual DateTime DateDebutEquilibrage
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateDebutFlexibilite (Date début flexibilité)
    /// </summary>
    /// <value>
    /// The date debut flexibilite.
    /// </value>
    public virtual DateTime DateDebutFlexibilite
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateDebutPEG (Date début PEG gré à gré)
    /// </summary>
    /// <value>
    /// The date debut peg.
    /// </value>
    public virtual DateTime DateDebutPEG
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateDebutPEGPowerNext (Date début PEG Power Next)
    /// </summary>
    /// <value>
    /// The date debut peg power next.
    /// </value>
    public virtual DateTime DateDebutPEGPowerNext
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateEffetAvenant (Date d'effet de l'avenant)
    /// </summary>
    /// <value>
    /// The date effet avenant.
    /// </value>
    public virtual DateTime DateEffetAvenant
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateExpirationCaution (Date expiration caution)
    /// </summary>
    /// <value>
    /// The date expiration caution.
    /// </value>
    public virtual DateTime DateExpirationCaution
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFin (Date fin du contrat)
    /// </summary>
    /// <value>
    /// The date fin.
    /// </value>
    public virtual DateTime DateFin
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFinEquilibrage (Date fin équilibrage)
    /// </summary>
    /// <value>
    /// The date fin equilibrage.
    /// </value>
    public virtual DateTime DateFinEquilibrage
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFinFlexibilite (Date fin flexibilité)
    /// </summary>
    /// <value>
    /// The date fin flexibilite.
    /// </value>
    public virtual DateTime DateFinFlexibilite
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFinPEG (Date fin PEG gré à gré)
    /// </summary>
    /// <value>
    /// The date fin peg.
    /// </value>
    public virtual DateTime DateFinPEG
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFinPEGPowerNext (Date fin PEG Power Next)
    /// </summary>
    /// <value>
    /// The date fin peg power next.
    /// </value>
    public virtual DateTime DateFinPEGPowerNext
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateMaj (Date de dernière MAJ)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignature (Date de signature)
    /// </summary>
    /// <value>
    /// The date signature.
    /// </value>
    public virtual DateTime DateSignature
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignatureEquilibrage (Date signature équilibrage)
    /// </summary>
    /// <value>
    /// The date signature equilibrage.
    /// </value>
    public virtual DateTime DateSignatureEquilibrage
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignatureFlexibilite (Date signature flexibilité)
    /// </summary>
    /// <value>
    /// The date signature flexibilite.
    /// </value>
    public virtual DateTime DateSignatureFlexibilite
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignaturePEG (Date signature PEG gré à gré)
    /// </summary>
    /// <value>
    /// The date signature peg.
    /// </value>
    public virtual DateTime DateSignaturePEG
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignaturePEGPowerNext (Date signature PEG Power Next)
    /// </summary>
    /// <value>
    /// The date signature peg power next.
    /// </value>
    public virtual DateTime DateSignaturePEGPowerNext
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _deviseFacturation (Devise de facturation)
    /// </summary>
    /// <value>
    /// The devise facturation.
    /// </value>
    public virtual string DeviseFacturation
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _emetteurCaution (Emetteur caution)
    /// </summary>
    /// <value>
    /// The emetteur caution.
    /// </value>
    public virtual string EmetteurCaution
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _client (Objet ClientComptable)
    /// </summary>
    /// <value>
    /// The expediteur transport.
    /// </value>
    public virtual VOExpediteurTransport ExpediteurTransport
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _jourPaiement (Jour de paiement)
    /// </summary>
    /// <value>
    /// The jour paiement.
    /// </value>
    public virtual int JourPaiement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _modePaiement (Code mode de paiement)
    /// </summary>
    /// <value>
    /// The mode paiement.
    /// </value>
    public virtual string ModePaiement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _moisContractuel (Mois de l'année contractuelle)
    /// </summary>
    /// <value>
    /// The mois contractuel.
    /// </value>
    public virtual int MoisContractuel
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _montantCaution (Montant caution)
    /// </summary>
    /// <value>
    /// The montant caution.
    /// </value>
    public virtual decimal MontantCaution
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _paiementSiNonOuvre (Paiement la veille si non ouvré)
    /// </summary>
    /// <value>
    /// The paiement si non ouvre.
    /// </value>
    public virtual bool PaiementSiNonOuvre
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _pk (Gestion Clé composé pour Hibernate)
    /// </summary>
    /// <value>
    /// The pk.
    /// </value>
    public virtual PKContrat Pk
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _preavisDenonciation (Préavis de dénonciation)
    /// </summary>
    /// <value>
    /// The preavis denonciation.
    /// </value>
    public virtual string PreavisDenonciation
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _premierJourFacturation (Premier jour de facturation)
    /// </summary>
    /// <value>
    /// The premier jour facturation.
    /// </value>
    public virtual int PremierJourFacturation
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _reconductionTacite (Reconduction tacite)
    /// </summary>
    /// <value>
    /// The reconduction tacite.
    /// </value>
    public virtual bool ReconductionTacite
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _soumissionPenalites (Soumission pénalités)
    /// </summary>
    /// <value>
    /// The soumission penalites.
    /// </value>
    public virtual bool SoumissionPenalites
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _statut (Statut du contrat)
    /// </summary>
    /// <value>
    /// The statut.
    /// </value>
    public virtual bool Statut
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _type (Type de contrat)
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public virtual string Type
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _typePaiement (Code type de paiement)
    /// </summary>
    /// <value>
    /// The type paiement.
    /// </value>
    public virtual string TypePaiement
    {
        get; set;
    }


    /// <summary>
    /// Accesseurs de l'attribut _dateDebutAcheminement (début acheminement transit ou livraison)
    /// </summary>
    /// <value>
    /// The type paiement.
    /// </value>
    public virtual DateTime DateDebutAcheminement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateFinAcheminement (fin acheminement transit ou livraison)
    /// </summary>
    /// <value>
    /// The type paiement.
    /// </value>
    public virtual DateTime DateFinAcheminement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _dateSignatureAcheminement (signature acheminement transit ou livraison)
    /// </summary>
    /// <value>
    /// The type paiement.
    /// </value>
    public virtual DateTime DateSignatureAcheminement
    {
        get; set;
    }

    /// <summary>
    /// Accesseurs de l'attribut _achmtTransitLiv
    /// </summary>
    public virtual bool AnnexePegGag
    {
        get
        {
            var result = false;
            var dateActuelle = DateTime.Now;

            if ((DateDebutPEG <= dateActuelle) && (DateFinPEG >= dateActuelle))
            {
                result = true;
            }

            return result;
        }
    }

    /// <summary>
    /// Indique si l'expéditeur fait de l'acheminement pour la date du jour
    /// </summary>
    public virtual bool AchmtTransitLiv
    {
        get
        {
            var result = false;
            var dateActuelle = DateTime.Now;

            if ((DateDebutAcheminement <= dateActuelle) && (DateFinAcheminement >= dateActuelle))
            {
                result = true;
            }

            return result;
        }
    }

    /// <summary>
    /// Indique si l'expéditeur fait de l'acheminement pour la date en paramètre
    /// </summary>
    public virtual bool AchmtTransitLivDate(DateTime jg)
    {
        var result = false;

        if ((DateDebutAcheminement <= jg) && (DateFinAcheminement >= jg))
        {
            result = true;
        }

        return result;
    }

    /// <summary>
    /// Indique si l'expéditeur fait de l'acheminement pour les dates indiquées
    /// </summary>
    /// <param name="dateDeb">
    /// La date de début.
    /// </param>
    /// <param name="dateFin">
    /// La date de fin.
    /// </param>
    /// <returns>
    /// The achmt transit liv date.
    /// </returns>
    public virtual bool AchmtTransitLivDate(DateTime dateDeb, DateTime dateFin)
    {
        return dateDeb >= this.DateDebutAcheminement && dateFin <= this.DateFinAcheminement;
    }


}

public class VOContratATR : VOContrat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VOContratATR"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOContratATR()
    {
        this.Tolerances = new ArrayList();
    }



    /// <summary>
    /// Accesseurs de l'attribut _tolerances (Collection d'objets TOLERANCE)
    /// </summary>
    /// <value>
    /// The tolerances.
    /// </value>
    public virtual IList Tolerances
    {
        get; set;
    }


}

public class VOContratATS : VOContrat
{

    /// <summary>
    /// Collection d'objets SOUSCRIPTION ATS
    /// </summary>
    private IList _souscriptionsATS;

    /// <summary>
    /// Collection d'objets SOUSCRIPTION SEJ
    /// </summary>
    private IList _souscriptionsSEJ;



    /// <summary>
    /// Initializes a new instance of the <see cref="VOContratATS"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOContratATS()
    {
        this._souscriptionsATS = new ArrayList();
        this._souscriptionsSEJ = new ArrayList();
    }


    /// <summary>
    /// Accesseurs de l'attribut _souscriptionsATS (Collection d'objets SOUSCRIPTION ATS)
    /// </summary>
    /// <value>
    /// The souscriptions ats.
    /// </value>
    public virtual IList SouscriptionsATS
    {
        get
        {
            return this._souscriptionsATS;
        }

        set
        {
            this._souscriptionsATS = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _souscriptionsSEJ (Collection d'objets SOUSCRIPTION SEJ)
    /// </summary>
    /// <value>
    /// The souscriptions sej.
    /// </value>
    public virtual IList SouscriptionsSEJ
    {
        get
        {
            return this._souscriptionsSEJ;
        }

        set
        {
            this._souscriptionsSEJ = value;
        }
    }


}

public class VOBilanJournalier
{

    private VOExpediteurTransport expTransport;

    /// <summary>
    /// Le code statut correspondant a l'etat Allocation Calculee
    /// </summary>
    public const string StatutAllocationCalculee = "AC";


    /// <summary>
    /// Le code statut correspondant a l'etat Allocation Envoyee
    /// </summary>
    public const string StatutAllocationEnvoyee = "AE";



    /// <summary>
    /// Initializes a new instance of the <see cref="VOBilanJournalier"/> class
    /// </summary>
    public VOBilanJournalier()
    {
        this.expTransport = new VOExpediteurTransport();
    }

    /// <summary>
    /// Achat Vente
    /// </summary>
    public virtual decimal AchatVente { get; set; }

    /// <summary>
    /// Quantit� Achat / Vente Stockage pour GC
    /// </summary>
    public virtual decimal AchatVenteStockage { get; set; }

    /// <summary>
    /// Achat Vente Stockage OD
    /// </summary>
    public virtual decimal AchatVenteStockageOD { get; set; }

    /// <summary>
    /// Achat Vente Stockage OE
    /// </summary>
    public virtual decimal AchatVenteStockageOE { get; set; }

    /// <summary>
    /// Active
    /// </summary>
    public virtual bool Actif { get; set; }

    /// <summary>
    /// Auteur mise � jour
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Commentaire
    /// </summary>
    public virtual string Commentaire { get; set; }

    /// <summary>
    /// Date de Bilan journalier
    /// </summary>
    public virtual DateTime DateBilan { get; set; }

    /// <summary>
    /// Date mise � jour
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    /// <summary>
    /// Details du bilan journalier
    /// </summary>
    public virtual IList DetailBilanJournalier { get; set; }

    /// <summary>
    /// Ecart cumul�
    /// </summary>
    public virtual decimal EcartCumule { get; set; }

    /// <summary>
    /// Ecart journalier
    /// </summary>
    public virtual decimal EcartJournalier { get; set; }

    /// <summary>
    /// Expediteur
    /// </summary>
    public virtual VOExpediteurTransport ExpTransport
    {
        get
        {
            return this.expTransport;
        }
        set
        {
            this.expTransport = value;
        }
    }

    /// <summary>
    /// Capacit� limite de transport 1 pour GC
    /// </summary>
    public virtual decimal Limite1 { get; set; }

    /// <summary>
    /// Limite 1 OD
    /// </summary>
    public virtual decimal Limite1OD { get; set; }

    /// <summary>
    /// Limite 1 OE
    /// </summary>
    public virtual decimal Limite1OE { get; set; }

    /// <summary>
    /// Capacit� limite de transport Pr�visionnel 1 pour GC
    /// </summary>
    public virtual decimal Limite1Partiel { get; set; }

    /// <summary>
    /// Limite 1P OD
    /// </summary>
    public virtual decimal Limite1PartielOD { get; set; }

    /// <summary>
    /// Limite 1P OE
    /// </summary>
    public virtual decimal Limite1PartielOE { get; set; }

    /// <summary>
    /// Capacit� limite de transport 2 pour GC
    /// </summary>
    public virtual decimal Limite2 { get; set; }

    /// <summary>
    /// Limite 2 OD
    /// </summary>
    public virtual decimal Limite2OD { get; set; }

    /// <summary>
    /// Limite 2 OE
    /// </summary>
    public virtual decimal Limite2OE { get; set; }

    /// <summary>
    /// Capacit� limite de transport Pr�visionnel 2 pour GC
    /// </summary>
    public virtual decimal Limite2Partiel { get; set; }

    /// <summary>
    /// Limite 2P OD
    /// </summary>
    public virtual decimal Limite2PartielOD { get; set; }

    /// <summary>
    /// Limite 2P OE
    /// </summary>
    public virtual decimal Limite2PartielOE { get; set; }

    /// <summary>
    /// Capacit� limite de transport 3 pour GC
    /// </summary>
    public virtual decimal Limite3 { get; set; }

    /// <summary>
    /// Limite 3 OD
    /// </summary>
    public virtual decimal Limite3OD { get; set; }

    /// <summary>
    /// Capacit� limite de transport Pr�visionnel 3 pour GC
    /// </summary>
    public virtual decimal Limite3Partiel { get; set; }

    /// <summary>
    /// Limite 3P OD
    /// </summary>
    public virtual decimal Limite3PartielOD { get; set; }

    /// <summary>
    /// Indique si les limites (J-2) ont �t� prise en compte pour le calcul du SEJ
    /// </summary>
    public virtual bool LimiteSej { get; set; }

    /// <summary>
    /// Indique si lors du calcul du bilan les limites et le stock de r�f�rence ont �t� recalcul�s
    /// </summary>
    public virtual bool LimiteStockReference { get; set; }

    /// <summary>
    /// Nb jours cons�cutifs d'utilisation du SP
    /// </summary>
    public virtual int NbJourConsUtilisationSP { get; set; }

    /// <summary>
    /// Nb jours utilisation du SP pour OD et GC
    /// </summary>
    public virtual int NbJourUtilisationSP { get; set; }

    /// <summary>
    /// Num�ro Bilan Journalier
    /// </summary>
    public virtual int Num { get; set; }

    /// <summary>
    /// N� Expediteur
    /// </summary>
    public virtual int NumExpediteur { get; set; }

    /// <summary>
    /// N� interne facture
    /// </summary>
    public virtual int NumInterneFacture { get; set; }

    /// <summary>
    /// Num�ro Ordre Bilan Journalier
    /// </summary>
    public virtual int NumOrdre { get; set; }

    /// <summary>
    /// Remise � zero
    /// </summary>
    public virtual decimal Raz { get; set; }

    /// <summary>
    /// Reajustement de stock
    /// </summary>
    public virtual decimal ReajustementStock { get; set; }

    /// <summary>
    /// Quantit� au titre du Service Ecart Journalier pour GC
    /// </summary>
    public virtual decimal Sej { get; set; }

    /// <summary>
    /// SEJ max pour GC
    /// </summary>
    public virtual decimal SejMax { get; set; }

    /// <summary>
    /// Sej max OD 
    /// </summary>
    public virtual decimal SejMaxOD { get; set; }

    /// <summary>
    /// Sej max OE 
    /// </summary>
    public virtual decimal SejMaxOE { get; set; }

    /// <summary>
    /// Service Equilibrage Jour OD
    /// </summary>
    public virtual decimal SejOD { get; set; }

    /// <summary>
    /// Service Equilibrage Jour OE
    /// </summary>
    public virtual decimal SejOE { get; set; }

    /// <summary>
    /// Soutirage de Pointe pour OD et GC
    /// </summary>
    public virtual decimal SoutiragePointe { get; set; }

    /// <summary>
    /// Soutirage de Pointe cumul� pour OD et GC
    /// </summary>
    public virtual decimal SoutiragePointeCumule { get; set; }

    /// <summary>
    /// Status bilan Journalier
    /// </summary>
    public virtual string Statut { get; set; }

    /// <summary>
    /// Stock Final pour GC
    /// </summary>
    public virtual decimal StockFinal { get; set; }

    /// <summary>
    /// Stock final OD
    /// </summary>
    public virtual decimal StockFinalOD { get; set; }

    /// <summary>
    /// Stock final OE
    /// </summary>
    public virtual decimal StockFinalOE { get; set; }

    /// <summary>
    /// Stock max pour GC
    /// </summary>
    public virtual decimal StockMax { get; set; }

    /// <summary>
    /// Stock Max OD
    /// </summary>
    public virtual decimal StockMaxOD { get; set; }

    /// <summary>
    /// Stock max OE
    /// </summary>
    public virtual decimal StockMaxOE { get; set; }

    /// <summary>
    /// Stock min pour GC
    /// </summary>
    public virtual decimal StockMin { get; set; }

    /// <summary>
    /// Stock min OD
    /// </summary>
    public virtual decimal StockMinOD { get; set; }

    /// <summary>
    /// Stock min OE
    /// </summary>
    public virtual decimal StockMinOE { get; set; }

    /// <summary>
    /// Stock de R�f�rence pour GC
    /// </summary>
    public virtual decimal StockReference { get; set; }

    /// <summary>
    /// Stock de reference OD
    /// </summary>
    public virtual decimal StockReferenceOD { get; set; }

    /// <summary>
    /// Stock de reference OE
    /// </summary>
    public virtual decimal StockReferenceOE { get; set; }

    /// <summary>
    /// Type M / J
    /// </summary>
    public virtual string TypeBilan { get; set; }

    /// <summary>
    /// Type de calcul
    /// </summary>
    public virtual int TypeCalcul { get; set; }
}

public class VOLimite
{
    /// <summary>
    /// Journée gazière
    /// </summary>
    public DateTime GasDay { get; set; }

    /// <summary>
    /// Numéro d'expéditeur
    /// </summary>
    public int ExpNum { get; set; }

    /// <summary>
    /// Stock minimal
    /// </summary>
    public decimal StockMin { get; set; }

    /// <summary>
    /// Stock maximal
    /// </summary>
    public decimal StockMax { get; set; }

    /// <summary>
    /// Capacité Limite de Transport WithDrawal (Soutirage) minimal
    /// </summary>
    public decimal CLTWithDrawalMin { get; set; }

    /// <summary>
    /// Capacité Limite de Transport WithDrawal (Soutirage) maximal
    /// </summary>
    public decimal CLTWithDrawalMax { get; set; }

    /// <summary>
    /// Capacité Limite de Transport ReducedWithDrawal (Soutirage réduit) minimal
    /// </summary>
    public decimal CLTReducedWithDrawalMin { get; set; }

    /// <summary>
    /// Capacité Limite de Transport ReducedWithDrawal (Soutirage réduit) maximal
    /// </summary>
    public decimal CLTReducedWithDrawalMax { get; set; }

    /// <summary>
    /// Capacité Limite de Transport Injection minimal
    /// </summary>
    public decimal CLTInjectionMin { get; set; }

    /// <summary>
    /// Capacité Limite de Transport Injection maximal
    /// </summary>
    public decimal CLTInjectionMax { get; set; }

    /// <summary>
    /// Capacité Limite de Transport ReducedInjection minimal
    /// </summary>
    public decimal CLTReducedInjectionMin { get; set; }

    /// <summary>
    /// Capacité Limite de Transport ReducedInjection maximal
    /// </summary>
    public decimal CLTReducedInjectionMax { get; set; }

    /// <summary>
    /// Stock de Référence utilisé
    /// </summary>
    public decimal SRUtilise { get; set; }

    /// <summary>
    /// Stock final utilisé
    /// </summary>
    public decimal SFinal { get; set; }

    /// <summary>
    /// Quantité Achat / Vente Stockage
    /// </summary>
    public decimal AchatVenteStockage { get; set; }

    /// <summary>
    /// SEJ Max
    /// </summary>
    //public decimal SEJMax { get; set; }

    /// <summary>
    /// Auteur mise à jour
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Stock référence date
    /// </summary>
    public DateTime SRUtiliseDate { get; set; }

    /// <summary>
    /// Achat vente stockage date
    /// </summary>
    public DateTime AchatVenteStockageDate { get; set; }

    /// <summary>
    /// Stock final date
    /// </summary>
    public DateTime SFinalDate { get; set; }
}

public class VOLimiteStockage
{
    /// <summary>
    /// Identifiant
    /// </summary>
    public virtual int Ident { get; set; }

    /// <summary>
    /// Journee Gaziere
    /// </summary>
    public virtual DateTime JourneeGaziere { get; set; }

    /// <summary>
    /// Numéro interne Expéditeur
    /// </summary>
    public virtual decimal ExpNum { get; set; }

    /// <summary>
    /// Stock min utilisé pour le calcul
    /// </summary>
    public virtual decimal StockMin { get; set; }

    /// <summary>
    /// Stock max utilisé pour le calcul
    /// </summary>
    public virtual decimal StockMax { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle d’Injection
    /// </summary>
    public virtual decimal CtrInj { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle d’Injection Hors Booster
    /// </summary>
    public virtual decimal CtrInjHorsBooster { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle d’Injection Booster
    /// </summary>
    public virtual decimal CtrInjBooster { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle de Soutirage
    /// </summary>
    public virtual decimal CtrSout { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle Limites Totales Maximale d’injection
    /// </summary>
    public virtual decimal CltMaxInj { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle Limites Totales Minimale d’injection
    /// </summary>
    public virtual decimal CltMinInj { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle Limites Totales Maximale de soutirage
    /// </summary>
    public virtual decimal CltMaxSout { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle Limites Totales Minimale de soutirage
    /// </summary>
    public virtual decimal CltMinSout { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle réduite en Injection
    /// </summary>
    public virtual decimal CtrInjRed { get; set; }

    /// <summary>
    /// Capacité Journalière contractuelle réduite en Soutirage
    /// </summary>
    public virtual decimal CtrSoutRed { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Maximale d’injection
    /// </summary>
    public virtual decimal CltMaxInjRed { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Maximale d’injection hors booster
    /// </summary>
    public virtual decimal CltMaxInjRedHorsBooster { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Maximale d’injection booster
    /// </summary>
    public virtual decimal CltMaxInjRedBooster { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Minimale d’injection
    /// </summary>
    public virtual decimal CltMinInjRed { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Maximale de soutirage
    /// </summary>
    public virtual decimal CltMaxSoutRed { get; set; }

    /// <summary>
    /// Capacités Journalière Opérationnelle réduite Limites Totales Minimale de soutirage
    /// </summary>
    public virtual decimal CltMinSoutRed { get; set; }

    /// <summary>
    /// Type de calcul (Provisoire ou Définitif)
    /// </summary>
    public virtual string Type { get; set; }

    /// <summary>
    /// Stock de référence utilisé pour le calcul
    /// </summary>
    public virtual decimal StockRef { get; set; }

    /// <summary>
    /// Stock final utilisé pour le calcul
    /// </summary>
    public virtual decimal StockFinal { get; set; }

    /// <summary>
    /// Statut Actif / Inactif de la réduction
    /// </summary>
    public virtual bool Actif { get; set; }

    /// <summary>
    /// Date de mise à jour
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    /// <summary>
    /// Auteur de la mise à jour
    /// </summary>
    public virtual string AutMaj { get; set; }
}

public class VORequestExporterLimite
{
    /// <summary>
    /// Les limites
    /// </summary>
    public IList<VOLimite> Limites { get; set; }

    /// <summary>
    /// Receiver
    /// </summary>
    public string Receiver { get; set; }

    /// <summary>
    /// Identifiant
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Contrat
    /// </summary>
    public string Contract { get; set; }

    /// <summary>
    /// Date de début de la période.
    /// </summary>
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date de fin de la période.
    /// </summary>
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Extensions
    /// </summary>
    public List<string> Extensions { get; set; }

    /// <summary>
    /// Numéros d'expéditeur
    /// </summary>
    public int Expnum { get; set; }

    /// <summary>
    /// Type Limites : (D)éfinitives ou (P)rovisoires
    /// </summary>
    public string TypeLimites { get; set; }
}

public static class UtilDate
{

    /// <summary>
    /// Cette fonction permet d'obtenir une liste de dates consécutives entre les dates "from" et "thru"
    /// </summary>
    /// <param name="from">
    /// Date de début de l'intervalle
    /// </param>
    /// <param name="thru">
    /// Date de fin de l'intervalle 
    /// </param>
    /// <returns>
    /// Une liste de dates consécutives entre les dates "from" et "thru"
    /// </returns>
    public static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
    {
        for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        {
            yield return day;
        }
    }
}

public class VORequestGenererLimite
{
    // Define the properties here to match the parameters used in the original code
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public IList<string> Expediteurs { get; set; }
    public IList<string> TypeExtensions { get; set; }

}

public partial class LimitsMessage
{
    /// <summary>
    /// Valeurs pour l'entete
    /// </summary>
    public class LimitsHeaderValue
    {
        private LimitsHeaderValue(string value) { Value = value; }

        /// <summary>
        /// La valeur
        /// </summary>
        public string Value { get; set; }

        /// <remarks/>
        public static LimitsHeaderValue DocName { get { return new LimitsHeaderValue("LIMITS"); } }
        /// <remarks/>
        public static LimitsHeaderValue DocVersion { get { return new LimitsHeaderValue("2014"); } }
        /// <remarks/>
        public static LimitsHeaderValue MessageVersion { get { return new LimitsHeaderValue("1"); } }
        /// <remarks/>
        public static LimitsHeaderValue Sender { get { return new LimitsHeaderValue("TIGF"); } }
        /// <remarks/>
        public static LimitsHeaderValue Version { get { return new LimitsHeaderValue("1"); } }
        /// <remarks/>
        public static LimitsHeaderValue DocType { get { return new LimitsHeaderValue("Storage"); } }
        /// <remarks/>
        public static LimitsHeaderValue DocStatus { get { return new LimitsHeaderValue("Definitive"); } }
        /// <remarks/>
        public static LimitsHeaderValue Contract { get { return new LimitsHeaderValue("CONTRAT"); } }
        /// <remarks/>
        public static LimitsHeaderValue ContractFamily { get { return new LimitsHeaderValue("SIATIC"); } }
        /// <remarks/>
        public static LimitsHeaderValue FormatDate { get { return new LimitsHeaderValue("yyyyMMdd"); } }
        /// <remarks/>
        public static LimitsHeaderValue FormatId { get { return new LimitsHeaderValue("{0}{1}A{2}"); } }
        /// <remarks/>
        public static LimitsHeaderValue CodificationReceiver { get { return new LimitsHeaderValue("TIGF"); } }
        /// <remarks/>
        public static LimitsHeaderValue FormatDateEnvoi { get { return new LimitsHeaderValue("dd/MM/yyyy"); } }
    }

    /// <summary>
    /// Valeurs pour le corps
    /// </summary>
    public class LimitsCorpsValue
    {
        private LimitsCorpsValue(string value) { Value = value; }

        /// <summary>
        /// La valeur
        /// </summary>
        public string Value { get; set; }

        /// <remarks/>
        public static LimitsCorpsValue Point { get { return new LimitsCorpsValue("PITS"); } }
        /// <remarks/>
        public static LimitsCorpsValue Injection { get { return new LimitsCorpsValue("CLT Injection"); } }
        /// <remarks/>
        public static LimitsCorpsValue ReducedInjection { get { return new LimitsCorpsValue("CLT Reduced Injection"); } }
        /// <remarks/>
        public static LimitsCorpsValue WithDrawal { get { return new LimitsCorpsValue("CLT Withdrawal"); } }
        /// <remarks/>
        public static LimitsCorpsValue ReducedWithDrawal { get { return new LimitsCorpsValue("CLT Reduced Withdrawal"); } }
        /// <remarks/>
        public static LimitsCorpsValue Stock { get { return new LimitsCorpsValue("Stock"); } }
    }
}

public class VORequestLireParametre
{
    #region Constants and Fields

    /// <summary>
    /// Date d'effet du paramètre recherché
    /// </summary>
    private DateTime dateEffet = DateTime.Now;

    /// <summary>
    /// Après date d'effet ou non
    /// </summary>
    private bool apresDateEffet = true;

    #endregion

    #region Properties

    /// <summary>
    /// Code du paramètre recherché
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Date d'effet du paramètre recherché
    /// </summary>
    public DateTime DateEffet
    {
        get
        {
            return this.dateEffet;
        }

        set
        {
            this.dateEffet = value;
        }
    }

    /// <summary>
    /// Famille du paramètre recherché
    /// </summary>
    public string Famille { get; set; }

    /// <summary>
    /// Après date d'effet ou non
    /// </summary>
    public bool ApresDateEffet
    {
        get
        {
            return this.apresDateEffet;
        }

        set
        {
            this.apresDateEffet = value;
        }
    }

    #endregion
}

public class FileCsv
{
    public List<string[]> Header { get; set; }
    public List<string[]> Body { get; set; }

    public FileCsv()
    {
        Header = new List<string[]>();
        Body = new List<string[]>();
    }
}

public class PKContratRaccordement
{
    #region Constants and Fields

    #endregion

    #region Constructors and Destructors

    #endregion

    #region Properties

    /// <summary>
    /// Numero de contrat
    /// </summary>
    public virtual int NumContrat { get; set; }

    /// <summary>
    /// Numero d'avenant
    /// </summary>
    public virtual int NumAvenant { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// The equals.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// -------------------------------------------------------
    /// OBLIGATOIRE POUR HIBERNATE
    /// -------------------------------------------------------
    /// <returns>
    /// The equals.
    /// </returns>
    public override bool Equals(object obj)
    {
        var comparePk = obj as PKContratRaccordement;
        if (comparePk == null)
        {
            return false;
        }

        return this.NumContrat == comparePk.NumContrat && this.NumAvenant == comparePk.NumAvenant;
    }

    /// <summary>
    /// The get hash code.
    /// </summary>
    /// <returns>
    /// The get hash code.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = GetType().GetHashCode();
            hash = (hash * 31) ^ this.NumContrat.GetHashCode();
            hash = (hash * 31) ^ this.NumAvenant.GetHashCode();

            return hash;
        }
    }

    /// <summary>
    /// The to string.
    /// </summary>
    /// <returns>
    /// The to string.
    /// </returns>
    public override string ToString()
    {
        return base.ToString();
    }

    #endregion

    #region Methods

    #endregion
}

public class VOContratRaccordement
{
    #region Constants and Fields

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets PkContratRaccordement.
    /// </summary>
    public virtual PKContratRaccordement PKContratRaccordement { get; set; }

    /// <summary>
    /// Gets or sets Client.
    /// </summary>
    public virtual VOPoint Client { get; set; }

    /// <summary>
    /// Gets or sets Client.
    /// </summary>
    public virtual int NumClientCodePic { get; set; }

    /// <summary>
    /// Gets or sets Date de debut.
    /// </summary>
    public virtual DateTime DateDebut { get; set; }

    /// <summary>
    /// Gets or sets Date de fin.
    /// </summary>
    public virtual DateTime DateFin { get; set; }

    /// <summary>
    /// Gets or sets Actif.
    /// </summary>
    public virtual bool Actif { get; set; }

    /// <summary>
    /// Gets or sets AuteurMaj.
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Gets or sets Date de Maj.
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    #endregion
}

public class Conversion
{
    #region Public Methods

    /// <summary>
    /// The chr.
    /// </summary>
    /// <param name="code">
    /// The code.
    /// </param>
    /// <returns>
    /// The chr.
    /// </returns>
    public static string CHR(int code)
    {
        char[] chars;
        byte[] bytes = new byte[] { (Byte)code };
        ASCIIEncoding ascii = new ASCIIEncoding();
        string retour = string.Empty;
        int charCount = ascii.GetCharCount(bytes, 0, 1);
        chars = new char[charCount];
        int charsDecodedCount = ascii.GetChars(bytes, 0, 1, chars, 0);
        foreach (char c in chars)
        {
            retour = c.ToString();
        }

        return retour;
    }

    /// <summary>
    /// Convertit un objet en booléen
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Booléen
    /// </returns>
    public static bool ConvertObjBool(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return false;
        }
        else
        {
            return Convert.ToBoolean(Convert.ToInt32(RuntimeHelpers.GetObjectValue(dtr)));
        }
    }

    /// <summary>
    /// Convertit un objet en DateTime
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// DateTime correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>MinValue sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static DateTime ConvertObjDate(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return DateTime.MinValue;
        }
        else
        {
            return Convert.ToDateTime(RuntimeHelpers.GetObjectValue(dtr));
        }
    }

    /// <summary>
    /// Convertit un objet en décimal
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Décimal correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>MinValue sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static decimal ConvertObjDec(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return decimal.MinValue;
        }
        else
        {
            return Convert.ToDecimal(RuntimeHelpers.GetObjectValue(dtr));
        }
    }

    /// <summary>
    /// Convertit un objet en décimal
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Décimal correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>0 sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static decimal ConvertObjDec0(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return 0;
        }
        else
        {
            // DEB CG 28/09/2006 : si pas null, mais vide (c'est subtil mais c'est pas pareil)

            if (string.IsNullOrEmpty(dtr.ToString()))
            {
                // GB 0927 30/04/2008 transformation de == string.Empty en string.IsNullOrEmpty(
                return 0;
            }
            else
            {
                // DEB 0502 GCE 09/12/2008
                NumberFormatInfo provider = new NumberFormatInfo();
                provider.NumberDecimalSeparator = ".";

                // return Convert.ToDecimal(RuntimeHelpers.GetObjectValue(dtr));
                return Convert.ToDecimal(dtr.ToString().Replace(",", "."), provider);
                // FIN 0502 GCE 09/12/2008
            }

            // FIN CG
        }
    }

    /// <summary>
    /// Convertit un objet en entier
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Entier correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>MinValue sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static int ConvertObjInt(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return int.MinValue;
        }
        else
        {
            return Convert.ToInt32(RuntimeHelpers.GetObjectValue(dtr));
        }
    }

    /// <summary>
    /// Convertit un objet en entier
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Entier correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>0 sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static int ConvertObjInt0(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return 0;
        }
        else
        {
            return Convert.ToInt32(RuntimeHelpers.GetObjectValue(dtr));
        }
    }

    /// <summary>
    /// Convertit un objet en entier long
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Entier long correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>MinValue sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static long ConvertObjLong(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return long.MinValue;
        }
        else
        {
            // DEB 0482 GC 12/02/2007
            // return Convert.ToInt32(RuntimeHelpers.GetObjectValue(dtr));
            return Convert.ToInt64(RuntimeHelpers.GetObjectValue(dtr));

            // FIN 0482 GC 12/02/2007
        }
    }

    /// <summary>
    /// Convertit un objet en entier long
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Entier long correspondant à 
    /// <list type="bullet">
    /// <item>
    /// <description>L'objet converti si il est défini</description>
    /// </item>
    /// <item>
    /// <description>0 sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static long ConvertObjLong0(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return 0;
        }
        else
        {
            // DEB 0482 GC 12/02/2007
            // return Convert.ToInt32(RuntimeHelpers.GetObjectValue(dtr));
            return Convert.ToInt64(RuntimeHelpers.GetObjectValue(dtr));

            // FIN 0482 GC 12/02/2007
        }
    }

    /// <summary>
    /// Convertit un objet en chaîne de caractères
    /// </summary>
    /// <param name="dtr">
    /// Objet à convertir
    /// </param>
    /// <returns>
    /// Chaîne de caractères
    /// </returns>
    public static string ConvertObjString(object dtr)
    {
        if (RuntimeHelpers.GetObjectValue(dtr) == System.DBNull.Value)
        {
            return string.Empty;
        }
        else
        {
            return Convert.ToString(RuntimeHelpers.GetObjectValue(dtr));
        }
    }

    /// <summary>
    /// Convertit un nombre décimal en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Nombre décimal à convertir
    /// </param>
    /// <returns>
    /// Nombre décimal sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>Null sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString(decimal obj)
    {
        if (obj == decimal.MinValue)
        {
            return "null";
        }
        else
        {
            return obj.ToString().Replace(",", ".");
        }
    }

    /// <summary>
    /// Convertit un objet DateTime en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// DateTime à convertir
    /// </param>
    /// <returns>
    /// Objet DateTime sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>"dd/MM/yyyy HH:mm:ss" si il est défini</description>
    /// </item>
    /// <item>
    /// <description>Null sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString(DateTime obj)
    {
        if (obj == DateTime.MinValue)
        {
            return "null";
        }
        else
        {
            return "to_date('" + obj.ToString("dd/MM/yyyy HH:mm:ss") + "','DD/MM/YYYY HH24:MI:SS')";
        }
    }

    /// <summary>
    /// Convertit un objet DateTime en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// DateTime à convertir
    /// </param>
    /// <param name="cache">
    /// Indique si la requete s'effectue sur un DataSet ou en BDD
    /// </param>
    /// <returns>
    /// The convert string.
    /// </returns>
    public static string convertString(DateTime obj, bool cache)
    {
        if (obj == DateTime.MinValue)
        {
            return "null";
        }
        else
        {
            if (cache)
            {
                return "'" + obj.Day + "/" + obj.Month + "/" + obj.Year + " " + obj.Hour + ":" + obj.Minute + ":" +
                       obj.Second + "'";
            }
            else
            {
                return "to_date('" + obj.ToString("dd/MM/yyyy HH:mm:ss") + "','DD/MM/YYYY HH24:MI:SS')";
            }
        }
    }

    /// <summary>
    /// The convert string.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// <param name="paramNum">
    /// The param num.
    /// </param>
    /// <param name="paramValue">
    /// The param value.
    /// </param>
    /// <returns>
    /// The convert string.
    /// </returns>
    public static string convertString(DateTime obj, int paramNum, out string paramValue)
    {
        if (obj == DateTime.MinValue)
        {
            paramValue = null;
            return "null";
        }
        else
        {
            paramValue = obj.ToString("dd/MM/yyyy HH:mm:ss");
            return "to_date(:" + paramNum.ToString() + ",'DD/MM/YYYY HH24:MI:SS')";
        }
    }

    /// <summary>
    /// Convertit un booléen en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Booléen à convertir
    /// </param>
    /// <returns>
    /// Booléen sous forme de chaîne de caractères (0/1)
    /// </returns>
    public static string convertString(bool obj)
    {
        if (obj == true)
        {
            return "1";
        }
        else
        {
            return "0";
        }
    }

    /// <summary>
    /// Convertit un entier en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Entier à convertir
    /// </param>
    /// <returns>
    /// Entier sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>Null sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString(int obj)
    {
        if (obj == int.MinValue)
        {
            return "null";
        }
        else
        {
            return obj.ToString();
        }
    }

    /// <summary>
    /// Convertit un entier long en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Entier long à convertir
    /// </param>
    /// <returns>
    /// Entier long sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>Null sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString(long obj)
    {
        if (obj == long.MinValue)
        {
            return "null";
        }
        else
        {
            return obj.ToString();
        }
    }

    /// <summary>
    /// Double le caractère "'" dans une chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Chaîne de caractères à traiter
    /// </param>
    /// <returns>
    /// Chaîne de caractères
    /// </returns>
    public static string convertString(string obj)
    {
        if (obj == string.Empty || obj == null)
        {
            return string.Empty;
        }
        else
        {
            return obj.Replace("'", "''");
        }
    }

    /// <summary>
    /// Convertit un nombre décimal en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Nombre décimal à convertir
    /// </param>
    /// <returns>
    /// Nombre décimal sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>"0" sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString0(decimal obj)
    {
        if (obj == decimal.MinValue)
        {
            return "0";
        }
        else
        {
            return obj.ToString().Replace(",", ".");
        }
    }

    /// <summary>
    /// Convertit un entier en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Entier à convertir
    /// </param>
    /// <returns>
    /// Entier sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>"0" sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString0(int obj)
    {
        if (obj == int.MinValue)
        {
            return "0";
        }
        else
        {
            return obj.ToString();
        }
    }

    /// <summary>
    /// Convertit un entier long en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// Entier long à convertir
    /// </param>
    /// <returns>
    /// Entier long sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>Chaîne de caractères si il est défini</description>
    /// </item>
    /// <item>
    /// <description>"0" sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertString0(long obj)
    {
        if (obj == long.MinValue)
        {
            return "0";
        }
        else
        {
            return obj.ToString();
        }
    }

    /// <summary>
    /// Convertit un objet DateTime en chaîne de caractères
    /// </summary>
    /// <param name="obj">
    /// DateTime à convertir
    /// </param>
    /// <returns>
    /// Objet DateTime sous la forme
    /// <list type="bullet">
    /// <item>
    /// <description>"dd/MM/yyyy" si il est défini</description>
    /// </item>
    /// <item>
    /// <description>Null sinon</description>
    /// </item>
    /// </list>
    /// </returns>
    public static string convertStringDMY(DateTime obj)
    {
        if (obj == DateTime.MinValue)
        {
            return "null";
        }
        else
        {
            return "to_date('" + obj.ToString("dd/MM/yyyy") + "','DD/MM/YYYY')";
        }
    }

    /// <summary>
    /// The convert string dmy.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// <param name="cache">
    /// The cache.
    /// </param>
    /// <returns>
    /// The convert string dmy.
    /// </returns>
    public static string convertStringDMY(DateTime obj, bool cache)
    {
        if (obj == DateTime.MinValue)
        {
            return "null";
        }
        else
        {
            if (cache)
            {
                return "'" + obj.Day + "/" + obj.Month + "/" + obj.Year + "'";
            }
            else
            {
                return "to_date('" + obj.ToString("dd/MM/yyyy") + "','DD/MM/YYYY')";
            }
        }
    }

    /// <summary>
    /// The convert string dmy.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// <param name="paramNum">
    /// The param num.
    /// </param>
    /// <param name="paramValue">
    /// The param value.
    /// </param>
    /// <returns>
    /// The convert string dmy.
    /// </returns>
    public static string convertStringDMY(DateTime obj, int paramNum, out string paramValue)
    {
        if (obj == DateTime.MinValue)
        {
            paramValue = null;
            return "null";
        }
        else
        {
            paramValue = obj.ToString("dd/MM/yyyy");
            return "to_date(:" + paramNum.ToString() + ",'DD/MM/YYYY')";
        }
    }

    #endregion
}

public class VOUtil
{
    #region Constants and Fields

    /// <summary>
    /// The type contrepartie_ distribution.
    /// </summary>
    public const string TypeContrepartie_Distribution = "ED";

    /// <summary>
    /// The type contrepartie_ grt.
    /// </summary>
    public const string TypeContrepartie_GRT = "GRT";

    /// <summary>
    /// Constante des Contreparties
    /// </summary>
    public const string TypeContrepartie_Transport = "ET";

    /// <summary>
    /// Constante de Type de Souscription
    /// </summary>
    public const string TypeSouscription_ATS = "ATS";

    /// <summary>
    /// The type souscription_ sej.
    /// </summary>
    public const string TypeSouscription_SEJ = "SEJ";

    #endregion
}

public class VOTypePoint
{
    #region Constants and Fields

    /// <summary>
    /// The aucun.
    /// </summary>
    public const string Aucun = "AUCUN";

    /// <summary>
    /// Réseau : RR/RP
    /// </summary>
    private string _reseau;

    /// <summary>
    /// Auteur mise à jour
    /// </summary>
    private string _typpautmaj;

    /// -------------------------------------------------------
    /// TABLE TYPPOINT
    /// -------------------------------------------------------
    /// <summary>
    /// Code type point
    /// </summary>
    private string _typpcod;

    /// <summary>
    /// Date mise à jour
    /// </summary>
    private DateTime _typpdtemaj;

    /// <summary>
    /// Libelle type point
    /// </summary>
    private string _typplib;

    /// <summary>
    /// Type point
    /// </summary>
    private string _typptyppt;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOTypePoint"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOTypePoint()
    {
        this._typpcod = String.Empty;
        this._typptyppt = String.Empty;
        this._typplib = String.Empty;
        this._reseau = String.Empty;
        this._typpautmaj = String.Empty;
        this._typpdtemaj = DateTime.MinValue;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Accesseurs de l'attribut _typpautmaj (Auteur mise à jour)
    /// </summary>
    /// <value>
    /// The auteur maj.
    /// </value>
    public virtual string AuteurMaj
    {
        get
        {
            return this._typpautmaj;
        }

        set
        {
            this._typpautmaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _typpcod (Code type point)
    /// </summary>
    /// <value>
    /// The code.
    /// </value>
    public virtual string Code
    {
        get
        {
            return this._typpcod;
        }

        set
        {
            this._typpcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _typpdtemaj (Date mise à jour)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get
        {
            return this._typpdtemaj;
        }

        set
        {
            this._typpdtemaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _typplib (Libelle type point)
    /// </summary>
    /// <value>
    /// The libelle.
    /// </value>
    public virtual string Libelle
    {
        get
        {
            return this._typplib;
        }

        set
        {
            this._typplib = value;
        }
    }

    // Debut PRO 23/04/08
    /// <summary>
    /// Accesseurs de l'attribut _reseau (Type de réseau)
    /// </summary>
    /// <value>
    /// The reseau.
    /// </value>
    public virtual string Reseau
    {
        get
        {
            return this._reseau;
        }

        set
        {
            this._reseau = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _typptyppt (Type point)
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public virtual string Type
    {
        get
        {
            return this._typptyppt;
        }

        set
        {
            this._typptyppt = value;
        }
    }

    #endregion

    // Fin PRO 23/04/08
}

public class VOPropObj
{
    #region Properties
    /// <summary>
    /// Auteur de mise à jour
    /// </summary>
    public virtual string Auteurmaj { get; set; }

    /// <summary>
    /// Code de la propriété
    /// </summary>
    public virtual string Codeprop { get; set; }

    /// <summary>
    /// Date de mise à jour
    /// </summary>
    public virtual DateTime Datemaj { get; set; }

    /// <summary>
    /// Format de la valeur de la propriété
    /// </summary>
    public virtual string Format { get; set; }

    /// <summary>
    /// Identifiant de la propriété
    /// </summary>
    public virtual int Idpropobj { get; set; }

    /// <summary>
    /// Libellé de la propriété
    /// </summary>
    public virtual string Libelle { get; set; }

    /// <summary>
    /// Type d'objet associé à la propriété
    /// </summary>
    public virtual string Typobj { get; set; }
    #endregion
}

public class VOProprietePoint
{
    #region Constants and Fields

    /// <summary>
    /// Auteur mise à jour
    /// </summary>
    private string _lppautmaj;

    /// <summary>
    /// Date de début
    /// </summary>
    private DateTime _lppdteefd;

    /// <summary>
    /// Date de fin
    /// </summary>
    private DateTime _lppdteeff;

    /// <summary>
    /// Date mise à jour
    /// </summary>
    private DateTime _lppdtemaj;

    /// <summary>
    /// Valeur de la propriété
    /// </summary>
    private string _lppvalpro;

    /// <summary>
    /// Objet point
    /// </summary>
    private VOPoint _point;

    /// <summary>
    /// Code propriété
    /// </summary>
    private string _ptprocod;

    /// <summary>
    /// Code point
    /// </summary>
    private string _ptrcod;

    /// <summary>
    /// Type Point réseau
    /// </summary>
    private string _typpcod;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOProprietePoint"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOProprietePoint()
    {
        this._ptprocod = String.Empty;
        this._point = new VOPoint();
        this._ptrcod = string.Empty;
        this._typpcod = string.Empty;
        this._lppdteefd = DateTime.MinValue;
        this._lppdteeff = DateTime.MinValue;
        this._lppvalpro = String.Empty;
        this._lppautmaj = String.Empty;
        this._lppdtemaj = DateTime.MinValue;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Accesseurs de l'attribut _lppautmaj (Auteur mise à jour)
    /// </summary>
    /// <value>
    /// The auteur maj.
    /// </value>
    public virtual string AuteurMaj
    {
        get
        {
            return this._lppautmaj;
        }

        set
        {
            this._lppautmaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _ptrcod (Code point)
    /// </summary>
    /// <value>
    /// The code point.
    /// </value>
    public virtual string CodePoint
    {
        get
        {
            return this._ptrcod;
        }

        set
        {
            this._ptrcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _ptprocod (Code propriété)
    /// </summary>
    /// <value>
    /// The code propriete.
    /// </value>
    public virtual string CodePropriete
    {
        get
        {
            return this._ptprocod;
        }

        set
        {
            this._ptprocod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _lppdteefd (Date de début)
    /// </summary>
    /// <value>
    /// The date debut.
    /// </value>
    public virtual DateTime DateDebut
    {
        get
        {
            return this._lppdteefd;
        }

        set
        {
            this._lppdteefd = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _lppdteeff (Date de fin)
    /// </summary>
    /// <value>
    /// The date fin.
    /// </value>
    public virtual DateTime DateFin
    {
        get
        {
            return this._lppdteeff;
        }

        set
        {
            this._lppdteeff = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _lppdtemaj (Date mise à jour)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get
        {
            return this._lppdtemaj;
        }

        set
        {
            this._lppdtemaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _point (Objet point)
    /// </summary>
    /// <value>
    /// The point.
    /// </value>
    public virtual VOPoint Point
    {
        get
        {
            return this._point;
        }

        set
        {
            this._point = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _typpcod (Type Point réseau)
    /// </summary>
    /// <value>
    /// The type point.
    /// </value>
    public virtual string TypePoint
    {
        get
        {
            return this._typpcod;
        }

        set
        {
            this._typpcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _lppvalpro (Valeur de la propriété)
    /// </summary>
    /// <value>
    /// The valeur propriete.
    /// </value>
    public virtual string ValeurPropriete
    {
        get
        {
            return this._lppvalpro;
        }

        set
        {
            this._lppvalpro = value;
        }
    }

    #endregion
}

public class VOLienPropObj
{
    #region Constants and Fields
    /// <summary>
    /// Identifiant de la propriété utilisé pour l'affichage
    /// </summary>
    private int identPropriete = int.MinValue;
    #endregion

    #region Properties
    /// <summary>
    /// Statut actif/inactif
    /// </summary>
    public virtual bool Active { get; set; }

    /// <summary>
    /// Auteur de mise à jour
    /// </summary>
    public virtual string Auteurmaj { get; set; }

    /// <summary>
    /// Type de l'objet
    /// </summary>
    public virtual string Codeobj { get; set; }

    /// <summary>
    /// Date d'effet de la valeur de la propriété
    /// </summary>
    public virtual DateTime Dateeff { get; set; }

    /// <summary>
    /// Date de mise à jour
    /// </summary>
    public virtual DateTime Datemaj { get; set; }

    /// <summary>
    /// Identifiant de la valeur de la propriété
    /// </summary>
    public virtual int Idlienpropobj { get; set; }

    /// <summary>
    /// Objet VOPropObj
    /// </summary>
    public virtual VOPropObj PropObj { get; set; }

    /// <summary>
    /// Valeur de la propriété
    /// </summary>
    public virtual string Valeur { get; set; }

    /// <summary>
    /// Clé de l'objet
    /// </summary>
    public string Key
    {
        get
        {
            return this.Idlienpropobj.ToString();
        }
    }

    /// <summary>
    /// Libellé de l'objet
    /// </summary>
    public string DisplayValue
    {
        get
        {
            return this.Valeur;
        }
    }

    /// <summary>
    /// Identifiant de la propriété utilisé pour l'affichage
    /// Le get permet d'utiliser l'identifiant de la propriété VOPropObj
    /// Le set permet de récupérer un nouvel identifiant de propriété saisi par un utilisateur
    /// </summary>
    public virtual int IdentPropriete
    {
        get
        {
            if (this.identPropriete == int.MinValue)
            {
                this.identPropriete = this.PropObj != null ? this.PropObj.Idpropobj : int.MinValue;
            }
            return this.identPropriete;
        }

        set
        {
            this.identPropriete = value;
        }
    }

    #endregion

    /// <summary>
    /// Initialisation des attributs a partir d'un DataRow
    /// </summary>
    /// <param name="dataRow">
    /// Enregistrement utilise pour initialiser les attributs de l'objet
    /// </param>
    /// <param name="dataRowVersion">
    /// Version de l'enregistrement a prendre en compte
    /// </param>
    protected internal void FromDataRow(DataRow dataRow, DataRowVersion dataRowVersion)
    {
        if (dataRow != null)
        {
            if (dataRow["IDLIENPROPOBJ"] != null && dataRow["IDLIENPROPOBJ"] != DBNull.Value)
            {
                this.Idlienpropobj = Convert.ToInt32(dataRow["IDLIENPROPOBJ"]);
            }
            else
            {
                this.Idlienpropobj = 0;
            }
            this.Valeur = dataRow["VALEUR"].ToString();
            this.Dateeff = Convert.ToDateTime(dataRow["DATEEFF"].ToString());
            this.Codeobj = dataRow["CODEOBJ"].ToString();
            this.Active = dataRow["ACTIF"] == DBNull.Value || Convert.ToBoolean(dataRow["ACTIF"]);
            this.PropObj = new VOPropObj();
            this.PropObj.Idpropobj = Convert.ToInt16(dataRow["TYPE"]);
        }
    }
}

public class VOPoint
{
    // Constant type de point
    #region Constants and Fields

    /// <summary>
    /// The typ pitd.
    /// </summary>
    public const string TypPitd = "PITD";

    /// <summary>
    /// The typ pl.
    /// </summary>
    public const string TypPl = "PL";

    /// <summary>
    /// Collection d’instance Allocation
    /// </summary>
    private IList allocations;

    /// <summary>
    /// Identifiant CLIMPACT
    /// </summary>
    private long? identifiantClimpact;

    /// <summary>
    /// Collection d’instance LienPointPoste
    /// </summary>
    private IList liensPointPoste;

    /// <summary>
    /// Collection d’instance PosteComptage
    /// </summary>
    private IList postes;

    /// <summary>
    /// Collection de PrixPoint
    /// </summary>
    private IList prixPointPe;

    /// <summary>
    /// Collection de ProprietePoint
    /// </summary>
    private IList proprietesPoint;

    /// <summary>
    /// Auteur mise à jour
    /// </summary>
    private string ptrautmaj;

    /// -------------------------------------------------------
    /// TABLE POINTRESEAU
    /// -------------------------------------------------------
    /// <summary>
    /// Code point
    /// </summary>
    private string ptrcod;

    /// <summary>
    /// Commentaire
    /// </summary>
    private string ptrcom;

    /// <summary>
    /// Date début de validité
    /// </summary>
    private DateTime ptrdtedeb;

    /// <summary>
    /// Date fin de validité
    /// </summary>
    private DateTime ptrdtefin;

    /// <summary>
    /// Date mise à jour
    /// </summary>
    private DateTime ptrdtemaj;

    /// <summary>
    /// Libellé du type de point
    /// </summary>
    private string ptrlib;

    /// <summary>
    /// Transmis SBT
    /// </summary>
    private bool transmissbt;

    /// <summary>
    /// Objet Type de point
    /// </summary>
    private VOTypePoint typePoint;

    /// <summary>
    /// Type de point
    /// </summary>
    private string typpcod;

    /// <summary>
    /// Chaine de caractère permettant d'accéder à la propriété code Prisma
    /// </summary>
    public static string ConstCodePrisma = "COD_PRISMA";
    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOPoint"/> class. 
    /// Constructeur vide 
    /// </summary>
    public VOPoint()
    {
        this.ptrcod = String.Empty;
        this.typePoint = new VOTypePoint();
        this.typpcod = string.Empty;
        this.proprietesPoint = new ArrayList();
        this.postes = new ArrayList();
        this.liensPointPoste = new ArrayList();
        this.allocations = new ArrayList();
        this.ptrlib = String.Empty;
        this.ptrcom = String.Empty;
        this.ptrdtedeb = DateTime.MinValue;
        this.ptrdtefin = DateTime.MinValue;
        this.transmissbt = false;
        this.ptrautmaj = String.Empty;
        this.ptrdtemaj = DateTime.MinValue;
    }
    #endregion

    #region Properties

    /// <summary>
    /// Accesseurs de l'attribut allocations (Collection d’instance Allocation)
    /// </summary>
    /// <value>
    /// The allocations.
    /// </value>
    public virtual IList Allocations
    {
        get
        {
            return this.allocations;
        }

        set
        {
            this.allocations = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrautmaj (Auteur mise à jour)
    /// </summary>
    /// <value>
    /// The auteur maj.
    /// </value>
    public virtual string AuteurMaj
    {
        get
        {
            return this.ptrautmaj;
        }

        set
        {
            this.ptrautmaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrcod (Code point)
    /// </summary>
    /// <value>
    /// The code.
    /// </value>
    public virtual string Code
    {
        get
        {
            return this.ptrcod;
        }

        set
        {
            this.ptrcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrcom (Commentaire)
    /// </summary>
    /// <value>
    /// The commentaire.
    /// </value>
    public virtual string Commentaire
    {
        get
        {
            return this.ptrcom;
        }

        set
        {
            this.ptrcom = value;
        }
    }

    /// <summary>
    /// Gets or sets Point.
    /// </summary>
    public virtual IList<VOContratRaccordement> ContratsRaccordement { get; set; }

    /// <summary>
    /// Accesseurs de l'attribut ptrdtedeb (Date début de validité)
    /// </summary>
    /// <value>
    /// The date debut.
    /// </value>
    public virtual DateTime DateDebut
    {
        get
        {
            return this.ptrdtedeb;
        }

        set
        {
            this.ptrdtedeb = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrdtefin (Date fin de validité)
    /// </summary>
    /// <value>
    /// The date fin.
    /// </value>
    public virtual DateTime DateFin
    {
        get
        {
            return this.ptrdtefin;
        }

        set
        {
            this.ptrdtefin = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrdtemaj (Date mise à jour)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get
        {
            return this.ptrdtemaj;
        }

        set
        {
            this.ptrdtemaj = value;
        }
    }

    /// <summary>
    /// Gets DisplayValue.
    /// </summary>
    /// <value>
    /// The display value.
    /// </value>
    public string DisplayValue
    {
        get
        {
            return this.ptrlib;
        }
    }

    /// <summary>
    /// Identifiant CLIMPACT
    /// </summary>
    public virtual long? IdentifiantClimpact
    {
        get
        {
            return this.identifiantClimpact;
        }

        set
        {
            this.identifiantClimpact = value;
        }
    }

    /// <summary>
    /// Gets or sets IdTetra.
    /// </summary>
    public virtual int IdTetra { get; set; }

    /// <summary>
    /// Gets Key.
    /// </summary>
    /// <value>
    /// The key.
    /// </value>
    public string Key
    {
        get
        {
            return this.ptrcod;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ptrlib (Libellé du type de point)
    /// </summary>
    /// <value>
    /// The libelle.
    /// </value>
    public virtual string Libelle
    {
        get
        {
            return this.ptrlib;
        }

        set
        {
            this.ptrlib = value;
        }
    }

    /// <summary>
    /// Collection d’instance LienPointPoste
    /// </summary>
    public virtual IList LiensPointPoste
    {
        get
        {
            return this.liensPointPoste;
        }

        set
        {
            this.liensPointPoste = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut postes (Collection d’instance PosteComptage)
    /// </summary>
    /// <value>
    /// The postes.
    /// </value>
    public virtual IList Postes
    {
        get
        {
            return this.postes;
        }

        set
        {
            this.postes = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut prixPointPe (Collection de PrixPoint)
    /// </summary>
    /// <value>
    /// The prices point.
    /// </value>
    public virtual IList PrixPointPE
    {
        get
        {
            return this.prixPointPe;
        }

        set
        {
            this.prixPointPe = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _props (Collection de ProprietePoint)
    /// </summary>
    /// <value>
    /// The proprietes point.
    /// </value>
    public virtual IList ProprietesPoint
    {
        get
        {
            return this.proprietesPoint;
        }

        set
        {
            this.proprietesPoint = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transmissbt (Transmis SBT)
    /// </summary>
    /// <value>
    /// The transmis sbt.
    /// </value>
    public virtual bool TransmisSBT
    {
        get
        {
            return this.transmissbt;
        }

        set
        {
            this.transmissbt = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut typpcod (Type point)
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public virtual string Type
    {
        get
        {
            if (this.typePoint != null && !string.IsNullOrEmpty(this.typePoint.Code))
            {
                return this.typePoint.Code;
            }
            return this.typpcod;
        }

        set
        {
            this.typpcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _type (Objet Type point)
    /// </summary>
    /// <value>
    /// The type point.
    /// </value>
    public virtual VOTypePoint TypePoint
    {
        get
        {
            return this.typePoint;
        }

        set
        {
            this.typePoint = value;
        }
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Teste si le point est de type PITD
    /// </summary>
    /// <returns>
    /// Vrai si le point est de type PITD
    /// </returns>
    public virtual bool IsTypePitd()
    {
        if (this.TypePoint.Type.Equals(TypPitd))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Teste si le point est de type PL
    /// </summary>
    /// <returns>
    /// Vrai si le point est de type PL
    /// </returns>
    public virtual bool IsTypePl()
    {
        if (this.TypePoint.Type.Equals(TypPl))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Récuperation de la valeur de l'objet sous forme de string
    /// </summary>
    /// <returns>
    /// Chaîne valeur
    /// </returns>
    public override string ToString()
    {
        return this.ptrcod;
    }

    /// <summary>
    /// Retourne le code PRISMA du point
    /// </summary>
    /// <returns>
    /// Code EIC
    /// </returns>
    public virtual string GetCodePrisma()
    {
        return this.GetCodePrisma(DateTime.Now.Date);
    }

    /// <summary>
    /// Retourne le code PRISMA du point
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Code EIC
    /// </returns>
    public virtual string GetCodePrisma(DateTime date)
    {
        return this.GetPropriete<string>(ConstCodePrisma, date);
    }

    /// <summary>
    /// Retourne le code PRISMA du point
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Code EIC
    /// </returns>
    public virtual string GetCodeStation(DateTime date)
    {
        return this.GetPropriete<string>("STATION", date);
    }

    /// <summary>
    /// Retourne le code EIC du point
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Code EIC
    /// </returns>
    public virtual string GetCodeEic(DateTime date)
    {
        return this.GetPropriete<string>("COD_EIC", date);
    }

    /// <summary>
    /// Retourne la propriété TYPE_COM du point
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Propriété TYPE_COM
    /// </returns>
    public virtual string GetTypeCom(DateTime date)
    {
        return this.GetPropriete<string>("TYPE_COM", date);
    }

    /// <summary>
    /// Retourne la propriété AGG_NOM du point (IsConfidential)
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Propriété AGG_NOM
    /// </returns>
    public virtual string GetConfidential(DateTime date)
    {
        return this.GetPropriete<string>("AGG_NOM", date);
    }

    /// <summary>
    /// Retourne la propriété TRANSCOD du point
    /// </summary>
    /// <param name="date">
    /// Date d'effet
    /// </param>
    /// <returns>
    /// Propriété TRANSCOD
    /// </returns>
    public virtual string GetTranCod(DateTime date)
    {
        return this.GetPropriete<string>("TRANSCOD", date);
    }

    /// <summary>
    /// Récupération d'une propriété de point valide à une date donnée
    /// </summary>
    /// <typeparam name="T">
    /// Type de la valeur de propriété
    /// </typeparam>
    /// <param name="code">
    /// Code de la propriété
    /// </param>
    /// <param name="date">
    /// Date de référence
    /// </param>
    /// <returns>
    /// Valeur de propriété
    /// </returns>
    private T GetPropriete<T>(string code, DateTime date)
    {
        var result = default(T);
        var p = (from prop in this.ProprietesPoint.Cast<VOProprietePoint>()
                 where prop.CodePropriete.Equals(code)
                    && prop.DateDebut <= date
                    && prop.DateFin >= date
                 select prop).FirstOrDefault();
        if (p != null)
        {
            result = (T)Convert.ChangeType(p.ValeurPropriete, typeof(T));
        }

        return result;
    }
    #endregion
}

public class VOPointPIPR : VOPoint
{
    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOPointPIPR"/> class. 
    /// 
    /// Constructeur vide 
    /// </summary>
    public VOPointPIPR()
    {
    }

    #endregion
}

public class VOTypeCorrespondant : VOCodeLibelle
{
}

public class VOCodeLibelle
{
    #region Properties

    /// <summary>
    /// Code
    /// </summary>
    public virtual string Code { get; set; }

    /// <summary>
    /// Libelle
    /// </summary>
    public virtual string Libelle { get; set; }

    #endregion
}

public class VOCorrespondant
{
    #region Properties

    /// <summary>
    /// Auteur de la mise à jour
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Date de la mise à jour
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    /// <summary>
    /// Email du correspondant
    /// </summary>
    public virtual string Email { get; set; }

    /// <summary>
    /// Id de l'adresse
    /// </summary>
    public virtual int IdCorrespondant { get; set; }

    /// <summary>
    /// Nom du correspondant
    /// </summary>
    public virtual string Nom { get; set; }

    /// <summary>
    /// Gets Nom Complet du correspondant
    /// </summary>
    public virtual string NomComplet
    {
        get
        {
            return String.Format("{0} {1}", this.Prenom, this.Nom);
        }
    }

    /// <summary>
    /// Prenom du correspondant
    /// </summary>
    public virtual string Prenom { get; set; }

    /// <summary>
    /// Telephone du correspondant
    /// </summary>
    public virtual string Telephone { get; set; }

    /// <summary>
    /// Type du contact (TP ,...)
    /// </summary>
    public virtual string TypeContact { get; set; }

    /// <summary>
    /// Url du correspondant
    /// </summary>
    public virtual string URL { get; set; }

    /// <summary>
    /// Commentaire
    /// </summary>
    public virtual string Comment { get; set; }

    /// <summary>
    /// CodeType
    /// </summary>
    public virtual VOTypeCorrespondant CodeType { get; set; }

    /// <summary>
    /// Titre
    /// </summary>
    public virtual string Titre { get; set; }

    /// <summary>
    /// Lieu
    /// </summary>
    public virtual string Lieu { get; set; }

    /// <summary>
    /// Fax
    /// </summary>
    public virtual string Fax { get; set; }

    /// <summary>
    /// ComplementAdresse
    /// </summary>
    public virtual string ComplementAdresse { get; set; }

    /// <summary>
    /// TelephonePortable
    /// </summary>
    public virtual string TelephonePortable { get; set; }

    /// <summary>
    /// RaccourciTelephoniqueTFE
    /// </summary>
    public virtual string RaccourciTelephoniqueTFE { get; set; }

    /// <summary>
    /// Civilite
    /// </summary>
    public virtual string Civilite { get; set; }

    /// <summary>
    /// NumeroExpediteurOuClient
    /// </summary>
    public virtual int NumeroExpediteurOuClient { get; set; }

    /// <summary>
    /// NumeroAdresse
    /// </summary>
    public virtual int NumeroAdresse { get; set; }

    /// <summary>
    /// IndicateurPrincipal
    /// </summary>
    public virtual int IndicateurPrincipal { get; set; }

    /// <summary>
    /// IndicateurTechnique
    /// </summary>
    public virtual int IndicateurTechnique { get; set; }

    #endregion
}

public class VOAdresse
{
    #region Constants and Fields

    /// <summary>
    /// Auteur de la mise à jour
    /// </summary>
    private string auteurMaj;

    /// <summary>
    /// Boite postale
    /// </summary>
    private string boitePostale;

    /// <summary>
    /// Pays du destinataire
    /// </summary>
    private string codePays;

    /// <summary>
    /// Code Postal
    /// </summary>
    private string codePostal;

    /// <summary>
    /// Date de la mise à jour
    /// </summary>
    private DateTime dateMaj;

    /// <summary>
    /// Id de l'adresse
    /// </summary>
    private int idAdresse;

    /// <summary>
    /// Nom du destinataire
    /// </summary>
    private string nom;

    /// <summary>
    /// Nom court du destinataire
    /// </summary>
    private string nomCourt;

    /// <summary>
    /// Numéro de l'adresse
    /// </summary>
    private string numero;

    /// <summary>
    /// Rue de l'adresse
    /// </summary>
    private string rue;

    /// <summary>
    /// Ville de l'adresse
    /// </summary>
    private string ville;
    #endregion

    #region Properties

    /// <summary>
    /// Auteur de la mise à jour
    /// </summary>
    public virtual string AuteurMaj
    {
        get
        {
            return this.auteurMaj;
        }

        set
        {
            this.auteurMaj = value;
        }
    }

    /// <summary>
    /// Gets or sets BoitePostale.
    /// </summary>
    public virtual string BoitePostale
    {
        get
        {
            return this.boitePostale;
        }

        set
        {
            this.boitePostale = value;
        }
    }

    /// <summary>
    /// Gets or sets CodePays.
    /// </summary>
    public virtual string CodePays
    {
        get
        {
            return this.codePays;
        }

        set
        {
            this.codePays = value;
        }
    }

    /// <summary>
    /// Gets or sets CodePostal.
    /// </summary>
    public virtual string CodePostal
    {
        get
        {
            return this.codePostal;
        }

        set
        {
            this.codePostal = value;
        }
    }

    /// <summary>
    /// Date de la mise à jour
    /// </summary>
    public virtual DateTime DateMaj
    {
        get
        {
            return this.dateMaj;
        }

        set
        {
            this.dateMaj = value;
        }
    }

    /// <summary>
    /// Id de l'adresse
    /// </summary>
    public virtual int IdAdresse
    {
        get
        {
            return this.idAdresse;
        }

        set
        {
            this.idAdresse = value;
        }
    }

    /// <summary>
    /// Gets or sets Nom.
    /// </summary>
    public virtual string Nom
    {
        get
        {
            return this.nom;
        }

        set
        {
            this.nom = value;
        }
    }

    /// <summary>
    /// Gets or sets NomCourt.
    /// </summary>
    public virtual string NomCourt
    {
        get
        {
            return this.nomCourt;
        }

        set
        {
            this.nomCourt = value;
        }
    }

    /// <summary>
    /// Gets or sets Numero.
    /// </summary>
    public virtual string Numero
    {
        get
        {
            return this.numero;
        }

        set
        {
            this.numero = value;
        }
    }

    /// <summary>
    /// Gets or sets Rue.
    /// </summary>
    public virtual string Rue
    {
        get
        {
            return this.rue;
        }

        set
        {
            this.rue = value;
        }
    }

    /// <summary>
    /// Gets or sets Ville.
    /// </summary>
    public virtual string Ville
    {
        get
        {
            return this.ville;
        }

        set
        {
            this.ville = value;
        }
    }

    #endregion
}

public class VOTransporteurNHibernate
{
    #region Constants and Fields

    /// <summary>
    /// Date d'effet
    /// </summary>
    private DateTime dateEffet = DateTime.Now;

    /// <summary>
    /// Collection d’instance ProprietesTransporteur
    /// </summary>
    private IList proprietesTransporteur;

    /// <summary>
    /// Collection d'objets ProprietesTransporteurActif
    /// </summary>
    private IList proprietesTransporteurActifs;

    /// <summary>
    /// Transmission fichier
    /// </summary>
    private int tanssort;

    /// <summary>
    /// Auteur mise à jour
    /// </summary>
    private string transautmaj;

    /// <summary>
    /// Code Abrege
    /// </summary>
    private string transcdabr;

    /// <summary>
    /// Code Transporteur
    /// </summary>
    private string transcod;

    /// <summary>
    /// Code contrat transporteur
    /// </summary>
    private string transcodctr;

    /// <summary>
    /// Date mise à jour
    /// </summary>
    private DateTime transdtemaj;

    /// <summary>
    /// The transfert.
    /// </summary>
    private int transfert;

    /// <summary>
    /// Libellé Transporteur
    /// </summary>
    private string translib;

    /// <summary>
    /// The transnum.
    /// </summary>
    private int transnum;

    /// <summary>
    /// Type Transporteur
    /// </summary>
    private string transtyp;

    /// <summary>
    /// Adresse du transporteur
    /// </summary>
    private VOAdresse adresse;

    /// <summary>
    /// Code Eic du transporteur
    /// </summary>
    private string codeEic;

    /// <summary>
    /// Liste des correspondant du transporteur
    /// </summary>
    private VOCorrespondant listeContacts;
    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOTransporteurNHibernate"/> class. 
    /// Constructeur vide 
    /// </summary>
    public VOTransporteurNHibernate()
    {
        this.transcod = String.Empty;
        this.transcdabr = String.Empty;
        this.translib = String.Empty;
        this.transtyp = String.Empty;
        this.tanssort = Int32.MinValue;
        this.transcodctr = String.Empty;
        this.transnum = Int32.MinValue;
        this.transfert = 0;
        this.transautmaj = String.Empty;
        this.transdtemaj = DateTime.MinValue;
    }
    #endregion

    #region Properties

    /// <summary>
    /// Adresse du transporteur
    /// </summary>
    public virtual VOAdresse Adresse
    {
        get
        {
            return this.adresse;
        }

        set
        {
            this.adresse = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transautmaj (Auteur mise à jour)
    /// </summary>
    /// <value>
    /// The auteur maj.
    /// </value>
    public virtual string AuteurMaj
    {
        get
        {
            return this.transautmaj;
        }

        set
        {
            this.transautmaj = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transcod (Code Transporteur)
    /// </summary>
    /// <value>
    /// The code.
    /// </value>
    public virtual string Code
    {
        get
        {
            return this.transcod;
        }

        set
        {
            this.transcod = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transcdabr (Code Abrege)
    /// </summary>
    /// <value>
    /// The code abrege.
    /// </value>
    public virtual string CodeAbrege
    {
        get
        {
            return this.transcdabr;
        }

        set
        {
            this.transcdabr = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transcodctr (Code contrat transporteur)
    /// </summary>
    /// <value>
    /// The code contrat.
    /// </value>
    public virtual string CodeContrat
    {
        get
        {
            return this.transcodctr;
        }

        set
        {
            this.transcodctr = value;
        }
    }

    /// <summary>
    /// Code Eic du transporteur
    /// </summary>
    public virtual string CodeEic
    {
        get
        {
            return this.codeEic;
        }

        set
        {
            this.codeEic = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transdtemaj (Date mise à jour)
    /// </summary>
    /// <value>
    /// The date maj.
    /// </value>
    public virtual DateTime DateMaj
    {
        get
        {
            return this.transdtemaj;
        }

        set
        {
            this.transdtemaj = value;
        }
    }




    /// <summary>
    /// Clé de l'objet
    /// </summary>
    public string Key
    {
        get
        {
            return this.Code;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut translib (Libellé Transporteur)
    /// </summary>
    /// <value>
    /// The libelle.
    /// </value>
    public virtual string Libelle
    {
        get
        {
            return this.translib;
        }

        set
        {
            this.translib = value;
        }
    }

    /// <summary>
    /// Liste des correspondant du transporteur
    /// </summary>
    public virtual VOCorrespondant ListeContacts
    {
        get
        {
            return this.listeContacts;
        }

        set
        {
            this.listeContacts = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transnum ()
    /// </summary>
    /// <value>
    /// The num.
    /// </value>
    public virtual int Num
    {
        get
        {
            return this.transnum;
        }

        set
        {
            this.transnum = value;
        }
    }

    /// <summary>
    /// Gets or sets ProprietesTransporteur.
    /// </summary>
    /// <value>
    /// The proprietes transporteur.
    /// </value>
    public virtual IList ProprietesTransporteur
    {
        get
        {
            return this.proprietesTransporteur;
        }

        set
        {
            this.proprietesTransporteur = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut ProprietesTransporteurctifs (Collection d'objets LienPropObj Actifs)
    /// </summary>
    /// <value>
    /// The proprietes transporteur actifs.
    /// </value>
    public virtual IList ProprietesTransporteurActifs
    {
        get
        {
            if (this.proprietesTransporteurActifs == null)
            {
                if (this.proprietesTransporteur != null)
                {
                    this.proprietesTransporteurActifs = new ArrayList();
                    foreach (VOLienPropObj proprietes in this.proprietesTransporteur)
                    {
                        if (proprietes.Active)
                        {
                            this.proprietesTransporteurActifs.Add(proprietes);
                        }
                    }
                }
            }

            return this.proprietesTransporteurActifs;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transfert ()
    /// </summary>
    /// <value>
    /// The transfert.
    /// </value>
    public virtual int Transfert
    {
        get
        {
            return this.transfert;
        }

        set
        {
            this.transfert = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut tanssort (Transmission fichier)
    /// </summary>
    /// <value>
    /// The transmission fichier.
    /// </value>
    public virtual int TransmissionFichier
    {
        get
        {
            return this.tanssort;
        }

        set
        {
            this.tanssort = value;
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut transtyp (Type Transporteur)
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public virtual string Type
    {
        get
        {
            return this.transtyp;
        }

        set
        {
            this.transtyp = value;
        }
    }

    /// <summary>
    /// Vrai si le transporteur reçoit des fichier
    /// </summary>
    public virtual bool ImporteFichier
    {
        get
        {
            VOLienPropObj modeCom = this.GetValeurProprieteActive("MODECOM", this.DateEffet);
            return modeCom == null || !modeCom.Valeur.Equals("0");
        }
    }

    /// <summary>
    /// Date d'effet
    /// </summary>
    public virtual DateTime DateEffet
    {
        get
        {
            return this.dateEffet;
        }

        set
        {
            this.dateEffet = value;
        }
    }
    #endregion

    #region Methods

    /// <summary>
    /// Initialisation des attributs a partir d'un DataRow
    /// </summary>
    /// <param name="dataRow">
    /// Enregistrement utilise pour initialiser les attributs de l'objet
    /// </param>
    /// <param name="dataRowVersion">
    /// Version de l'enregistrement a prendre en compte
    /// </param>
    protected internal void FromDataRow(DataRow dataRow, DataRowVersion dataRowVersion)
    {
        if (dataRow != null)
        {
            if (dataRow["TRANSNUM"] != null && dataRow["TRANSNUM"] != DBNull.Value)
            {
                this.Num = Convert.ToInt32(dataRow["TRANSNUM"].ToString());
            }
            {
                this.Num = 0;
            }
            this.Code = dataRow["TRANSCOD"].ToString();
            this.CodeAbrege = dataRow["TRANSCDABR"].ToString();
            this.Libelle = dataRow["TRANSLIB"].ToString();
            this.Type = "T";
            this.TransmissionFichier = Convert.ToInt32(dataRow["TANSSORT"].ToString());
            this.CodeContrat = dataRow["TRANSCODCTR"].ToString();
            this.DateMaj = DateTime.Now;
            this.CodeEic = dataRow["CODEEIC"].ToString();
        }
    }

    /// <summary>
    /// Récupération d'une valeur de propriété active pour une date d'effet donnée
    /// </summary>
    /// <param name="codeProp">Code de propriété</param>
    /// <param name="date">Date d'effet</param>
    /// <returns>Valeur de propriété</returns>
    protected VOLienPropObj GetValeurProprieteActive(string codeProp, DateTime date)
    {
        VOLienPropObj propObj = null;
        foreach (VOLienPropObj prop in this.ProprietesTransporteurActifs)
        {
            if (prop.PropObj.Codeprop.Equals(codeProp) && prop.Dateeff <= date)
            {
                if (propObj == null || propObj.Dateeff < prop.Dateeff)
                {
                    propObj = prop;
                }
            }
        }

        return propObj;
    }
    #endregion
}

public class VOTransporteurGRT : VOTransporteurNHibernate
{
    #region Constants and Fields

    /// <summary>
    /// GRT Espagne
    /// </summary>
    public const string Espagne = "Espagne";

    /// <summary>
    /// GRTGaz
    /// </summary>
    public const string GrtGaz = "GDFT";

    /// <summary>
    /// PITT
    /// </summary>
    private IList listePITT;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VOTransporteurGRT"/> class. 
    /// </summary>
    public VOTransporteurGRT()
    {
        this.listePITT = new ArrayList();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Accesseur de l'interface IContrepartie
    /// </summary>
    /// <value>
    /// The id contrepartie.
    /// </value>
    public virtual string IdContrepartie
    {
        get
        {
            return this.Code;
        }

        set
        {
            this.Code = Conversion.ConvertObjString(value);
        }
    }

    /// <summary>
    /// Accesseurs de l'attribut _pitt (PITT)
    /// </summary>
    /// <value>
    /// The liste pitt.
    /// </value>
    public virtual IList ListePITT
    {
        get
        {
            return this.listePITT;
        }

        set
        {
            this.listePITT = value;
        }
    }

    /// <summary>
    /// Liste de PIPR
    /// </summary>
    public virtual IList<VOPointPIPR> ListePIPR { get; set; }

    /// <summary>
    /// Accesseur de l'interface IContrepartie
    /// </summary>
    /// <value>
    /// The type contrepartie.
    /// </value>
    public virtual string TypeContrepartie
    {
        get
        {
            return VOUtil.TypeContrepartie_GRT;
        }
    }

    /// <summary>
    /// Vrai si le transporteur réalise le matching
    /// </summary>
    public virtual bool RealiseMatching
    {
        get
        {
            VOLienPropObj propObj = this.GetValeurProprieteActive("REALISANT_MATCHING", this.DateEffet);
            ////DEB 3042 GCE 01/09/2011
            ////return propObj == null || propObj.Valeur.Equals("1");
            return propObj != null && propObj.Valeur.Equals("1");
            ////FIN 3042 GCE 01/09/2011
        }
    }

    /// <summary>
    /// Vrai si le transporteur est espagnol
    /// </summary>
    public virtual bool IsEspagne
    {
        get
        {
            return this.Code.Equals(VOTransporteurGRT.Espagne);
        }
    }

    /// <summary>
    /// Libellé du GRT pour l'OBA
    /// </summary>
    public virtual string LibelleOba
    {
        get
        {
            return this.Code.Equals(VOTransporteurGRT.Espagne) ? "ESPAGNE" : "GRTGAZ";
        }
    }
    #endregion
}

public class VOCodificationExpediteur
{
    #region Constants and Fields

    /// <summary>
    /// Code du transporteur GRT
    /// </summary>
    private string transCode;

    /// <summary>
    /// Numéro du transporteur GRT
    /// </summary>
    private int? transNum;

    /// <summary>
    /// Numéro de l'expéditeur
    /// </summary>
    private int? expnum;
    #endregion

    #region Properties

    /// <summary>
    /// Vrai si l'objet est actif
    /// </summary>
    public virtual bool Actif { get; set; }

    /// <summary>
    /// Auteur de mise à jour
    /// </summary>
    public virtual string AuteurMaj { get; set; }

    /// <summary>
    /// Codification
    /// </summary>
    public virtual string Codification { get; set; }

    /// <summary>
    /// Date d'effet
    /// </summary>
    public virtual DateTime DateEffet { get; set; }

    /// <summary>
    /// Date de mise à jour
    /// </summary>
    public virtual DateTime DateMaj { get; set; }

    /// <summary>
    /// Numéro de l'expéditeur
    /// </summary>
    public virtual int ExpNum
    {
        get
        {
            if (this.expnum == null && this.Expediteur != null)
            {
                this.expnum = this.Expediteur.Num;
            }
            return this.expnum ?? int.MinValue;
        }

        set
        {
            this.expnum = value;
        }
    }

    /// <summary>
    /// Numéro expéditeur
    /// </summary>
    public virtual VOExpediteurTransport Expediteur { get; set; }

    /// <summary>
    /// Identifiant
    /// </summary>
    public virtual int Ident { get; set; }

    /// <summary>
    /// Code du transporteur GRT
    /// </summary>
    public virtual string TransCode
    {
        get
        {
            if (String.IsNullOrEmpty(this.transCode) && this.Transporteur != null)
            {
                this.transCode = this.Transporteur.Code;
            }
            return this.transCode;
        }

        set
        {
            this.transCode = value;
        }
    }

    /// <summary>
    /// Numéro du transporteur GRT
    /// </summary>
    public virtual int TransNum
    {
        get
        {
            if (this.transNum == null && this.Transporteur != null)
            {
                this.transNum = this.Transporteur.Num;
            }
            return this.transNum ?? int.MinValue;
        }

        set
        {
            this.transNum = value;
        }
    }

    /// <summary>
    /// Transporteur GRT
    /// </summary>
    public virtual VOTransporteurGRT Transporteur { get; set; }
    #endregion
}

#endregion

public class FunctionExporter
{
    #region Publication du fichier
    public static void PublierLimites(VORequestExporterLimite requestExporterLimite, string bucketName, string connectionPostgres, ILambdaContext context)
    {
        if (requestExporterLimite == null ||
            requestExporterLimite.Extensions == null ||
            requestExporterLimite.Extensions.Count <= 0)
        {
            return;
        }

        var receiver = GetReceiver(requestExporterLimite.Expnum, connectionPostgres, context);
        requestExporterLimite.Contract = GetContrat(receiver, connectionPostgres);
        requestExporterLimite.Receiver = receiver;
        requestExporterLimite.Id = GetId(connectionPostgres);

        foreach (var typeExtension in requestExporterLimite.Extensions)
        {
            switch (typeExtension)
            {
                case "CSV":
                    string keyNameXls = GetFileName(requestExporterLimite, "xls");
                    var fileCsv = ConvertDataCsv(requestExporterLimite);
                    ExportToS3Csv(fileCsv, bucketName, keyNameXls, context);
                    break;
                case "XML":
                    string keyNameXlm = GetFileName(requestExporterLimite, "xml");
                    var fileXml = ConvertDataToXml(requestExporterLimite);
                    ExportToS3Xml(fileXml, bucketName, keyNameXlm, context);
                    break;
                default:
                    string keyName = GetFileName(requestExporterLimite, "xls");
                    ExportToS3Csv(ConvertDataCsv(requestExporterLimite), bucketName, keyName, context);
                    break;
            }
        }
    }

    public static string GetFileName(VORequestExporterLimite requestExporterLimite, string format)
    {
        var dateAIndiquer = string.Empty;
        if (requestExporterLimite != null)
        {
            dateAIndiquer = requestExporterLimite.DateDebut.Equals(requestExporterLimite.DateFin)
                                ? string.Format(
                                    "J-{0}",
                                    requestExporterLimite.DateDebut.ToString("yyyyMMdd"))
                                : string.Format(
                                    "M-{0}",
                                    String.Concat(
                                    //"Du",
                                    //requestExporterLimite.DateDebut.ToString("yyyyMMdd"),
                                    //"Au",
                                    requestExporterLimite.DateFin.ToString("yyyyMMdd")));
        }

        var nomFichier = string.Format(
            "Limites-{0}-{1}-{2}." + format,
            requestExporterLimite == null ? string.Empty : requestExporterLimite.Receiver,
            dateAIndiquer,
            DateTime.Now.ToUniversalTime().ToString("ddMMyyyyhhmmss"));

        return nomFichier;
    }

    public static FileCsv ConvertDataCsv(VORequestExporterLimite voRequestExporterLimite)
    {
        if (voRequestExporterLimite.Limites == null || voRequestExporterLimite.Limites.Count <= 0)
        {
            return null;
        }

        var fichierCSV = new FileCsv();

        // Entete
        fichierCSV.Header.Add(new[] { "Limit", "Sender", "Receiver", "Doc Date", "Doc Number" });

        fichierCSV.Header.Add(
            new[]
            {
                    string.Empty,
                    LimitsMessage.LimitsHeaderValue.Sender.Value,
                    voRequestExporterLimite.Receiver,
                    DateTime.Now.ToUniversalTime().ToString(LimitsMessage.LimitsHeaderValue.FormatDateEnvoi.Value),
                    voRequestExporterLimite.Id
            });

        fichierCSV.Header.Add(null);

        fichierCSV.Body.Add(new[] { "Gas day", "Limit", "Min", "Max" });

        // Corps
        fichierCSV.Body.Add(null);

        foreach (var limite in voRequestExporterLimite.Limites)
        {
            fichierCSV.Body.Add(
                new[]
                {
                        limite.GasDay.ToString("dd/MM/yyyy"), "Stock", limite.StockMin.ToString(),
                        limite.StockMax.ToString()
                });
            fichierCSV.Body.Add(
                new[]
                {
                        limite.GasDay.ToString("dd/MM/yyyy"), "CLT Withdrawal", limite.CLTWithDrawalMin.ToString(),
                        limite.CLTWithDrawalMax.ToString()
                });
            fichierCSV.Body.Add(
                new[]
                {
                        limite.GasDay.ToString("dd/MM/yyyy"), "CLT Injection", limite.CLTInjectionMin.ToString(),
                        limite.CLTInjectionMax.ToString()
                });
            fichierCSV.Body.Add(
                new[]
                {
                        limite.GasDay.ToString("dd/MM/yyyy"), "CLT Reduced Withdrawal",
                        limite.CLTReducedWithDrawalMin.ToString(), limite.CLTReducedWithDrawalMax.ToString()
                });
            fichierCSV.Body.Add(
                new[]
                {
                        limite.GasDay.ToString("dd/MM/yyyy"), "CLT Reduced Injection",
                        limite.CLTReducedInjectionMin.ToString(), limite.CLTReducedInjectionMax.ToString()
                });
            fichierCSV.Body.Add(
                new[]
                {
                        limite.SRUtiliseDate.ToString("dd/MM/yyyy"), "SR", string.Empty,
                        limite.SRUtilise.ToString()
                });
        }
        return fichierCSV;
    }

    public static string ConvertDataToXml(VORequestExporterLimite voRequestExporterLimite)
    {
        if (voRequestExporterLimite == null || voRequestExporterLimite.Limites == null || voRequestExporterLimite.Limites.Count <= 0)
        {
            return null;
        }

        // Créez un objet XmlDocument pour construire le document XML
        XmlDocument xmlDoc = new XmlDocument();

        // Créez un élément racine
        XmlElement rootElement = xmlDoc.CreateElement("Data");

        // Ajoutez l'en-tête XML
        XmlElement headerElement = xmlDoc.CreateElement("Header");
        XmlElement headerItemElement = xmlDoc.CreateElement("HeaderItem");

        // Remplissez les éléments d'en-tête avec les données appropriées
        headerItemElement.SetAttribute("Limit", "");
        headerItemElement.SetAttribute("Sender", LimitsMessage.LimitsHeaderValue.Sender.Value);
        headerItemElement.SetAttribute("Receiver", voRequestExporterLimite.Receiver);
        headerItemElement.SetAttribute("DocDate", DateTime.Now.ToUniversalTime().ToString(LimitsMessage.LimitsHeaderValue.FormatDateEnvoi.Value));
        headerItemElement.SetAttribute("DocNumber", voRequestExporterLimite.Id);

        headerElement.AppendChild(headerItemElement);
        rootElement.AppendChild(headerElement);

        // Ajoutez le corps XML
        XmlElement bodyElement = xmlDoc.CreateElement("Body");

        // Ajoutez l'en-tête du corps
        XmlElement bodyHeaderElement = xmlDoc.CreateElement("BodyHeader");
        bodyHeaderElement.SetAttribute("GasDay", "");
        bodyHeaderElement.SetAttribute("Limit", "");
        bodyHeaderElement.SetAttribute("Min", "");
        bodyHeaderElement.SetAttribute("Max", "");

        bodyElement.AppendChild(bodyHeaderElement);

        foreach (var limite in voRequestExporterLimite.Limites)
        {
            XmlElement limiteElement = xmlDoc.CreateElement("Stock");
            limiteElement.SetAttribute("GasDay", limite.GasDay.ToString("dd/MM/yyyy"));
            limiteElement.SetAttribute("Stock", "Stock");
            limiteElement.SetAttribute("Min", limite.StockMin.ToString());
            limiteElement.SetAttribute("Max", limite.StockMax.ToString());
            bodyElement.AppendChild(limiteElement);

            XmlElement limiteElementCLTW = xmlDoc.CreateElement("CLTW");
            limiteElementCLTW.SetAttribute("GasDay", limite.GasDay.ToString("dd/MM/yyyy"));
            limiteElementCLTW.SetAttribute("Stock", "CLT Withdrawal");
            limiteElementCLTW.SetAttribute("Min", limite.CLTWithDrawalMin.ToString());
            limiteElementCLTW.SetAttribute("Max", limite.CLTWithDrawalMax.ToString());
            bodyElement.AppendChild(limiteElementCLTW);

            XmlElement limiteElementCLTI = xmlDoc.CreateElement("CLTI");
            limiteElementCLTI.SetAttribute("GasDay", limite.GasDay.ToString("dd/MM/yyyy"));
            limiteElementCLTI.SetAttribute("Stock", "CLT Injection");
            limiteElementCLTI.SetAttribute("Min", limite.CLTInjectionMin.ToString());
            limiteElementCLTI.SetAttribute("Max", limite.CLTInjectionMax.ToString());
            bodyElement.AppendChild(limiteElementCLTI);

            XmlElement limiteElementCLTRW = xmlDoc.CreateElement("CLTRW");
            limiteElementCLTRW.SetAttribute("GasDay", limite.GasDay.ToString("dd/MM/yyyy"));
            limiteElementCLTRW.SetAttribute("Stock", "CLT Reduced Withdrawal");
            limiteElementCLTRW.SetAttribute("Min", limite.CLTReducedWithDrawalMin.ToString());
            limiteElementCLTRW.SetAttribute("Max", limite.CLTReducedWithDrawalMax.ToString());
            bodyElement.AppendChild(limiteElementCLTRW);

            XmlElement limiteElementCLTRI = xmlDoc.CreateElement("CLTRI");
            limiteElementCLTRI.SetAttribute("GasDay", limite.GasDay.ToString("dd/MM/yyyy"));
            limiteElementCLTRI.SetAttribute("Stock", "CLT Reduced Injection");
            limiteElementCLTRI.SetAttribute("Min", limite.CLTReducedInjectionMin.ToString());
            limiteElementCLTRI.SetAttribute("Max", limite.CLTReducedInjectionMax.ToString());
            bodyElement.AppendChild(limiteElementCLTRI);

            XmlElement limiteElementSR = xmlDoc.CreateElement("SR");
            limiteElementSR.SetAttribute("GasDay", limite.SRUtiliseDate.ToString("dd/MM/yyyy"));
            limiteElementSR.SetAttribute("Stock", "SR");
            limiteElementSR.SetAttribute("Min", string.Empty);
            limiteElementSR.SetAttribute("Max", limite.SRUtilise.ToString());
            bodyElement.AppendChild(limiteElementSR);
        }

        rootElement.AppendChild(bodyElement);
        xmlDoc.AppendChild(rootElement);

        // Convertissez XmlDocument en une chaîne XML
        StringWriter stringWriter = new StringWriter();
        XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
        xmlDoc.WriteTo(xmlTextWriter);

        return stringWriter.ToString();
    }

    public static string GetContrat(string receiver, string connectionPostgres)
    {
        string paramValue = null;

        using (var connection = new NpgsqlConnection(connectionPostgres))
        {
            connection.Open();

            // Exécution de la requête SQL pour récupérer la date bascule
            string sql = "SELECT parval FROM ADM.PARDIV WHERE parcod = @Code AND pardteeff = @DateEffet and famcod = @Famille ;";
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("Code", LimitsMessage.LimitsHeaderValue.Contract.Value);
                command.Parameters.AddWithValue("DateEffet", DateTime.Now);
                command.Parameters.AddWithValue("Famille", LimitsMessage.LimitsHeaderValue.ContractFamily.Value);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    paramValue = Convert.ToString(result);
                }
            }
        }

        return string.Concat((paramValue == null) ? string.Empty : paramValue, receiver);
    }

    public static string GetId(string connectionPostgres)
    {
        // Consommation de la séquence
        var voRequestPublierSequence = new VORequestPublierSequence();
        voRequestPublierSequence.SetCle(LimitsMessage.LimitsHeaderValue.DocName.Value, DateTime.Now.ToUniversalTime().ToString(LimitsMessage.LimitsHeaderValue.FormatDate.Value));

        //var voSeq = BllSequence.ConsommerSequence(voRequestPublierSequence, null);

        var voSeq = new VOSequence() { };

        if (voRequestPublierSequence != null && voRequestPublierSequence.VoSequence != null)
        {
            using (var connection = new NpgsqlConnection(connectionPostgres))
            {
                connection.Open();

                // Lecture des séquences
                string sql = "SELECT valeur FROM adm.sequences where actif = @Actif and cle IN ('" +  voRequestPublierSequence.VoSequence.Cle + "')";
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("Actif", Convert.ToInt32(voRequestPublierSequence.VoSequence.Actif));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            voSeq.Valeur = reader.GetInt32(0);
                        }
                    }
                }
            }

            if (voSeq != null)
            {
                voSeq.Valeur += 1;
            }
            else
            {
                voSeq = voRequestPublierSequence.VoSequence;
                voSeq.Valeur = 1;
            }

            //DalSequence.CreateOrUpdateSequence(voSeq, localContext);

        }
        return string.Format(LimitsMessage.LimitsHeaderValue.FormatId.Value, // Format
                                LimitsMessage.LimitsHeaderValue.DocName.Value,
                                DateTime.Now.ToUniversalTime().ToString(LimitsMessage.LimitsHeaderValue.FormatDate.Value), voSeq);
    }

    public static string GetReceiver(int expnum, string connectionPostgres, ILambdaContext context)
    {
        // Lecture des codifications
        var transporteurList = new List<VOTransporteurGRT>();
        using (var connection = new NpgsqlConnection(connectionPostgres))
        {
            connection.Open();

            // Lecture du transporteur
            string sql = "SELECT * FROM adm.transporteur where transcod = '" + LimitsMessage.LimitsHeaderValue.CodificationReceiver.Value + "';";
            using (var command = new NpgsqlCommand(sql, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var transporteur = new VOTransporteurGRT
                        {
                            Num = Convert.ToInt32(reader["TRANSNUM"])
                        };

                        transporteurList.Add(transporteur);
                    }
                }
            }
        }

        // Lecture des codifications
        var codifExpList = new List<VOCodificationExpediteur>();
        using (var connection = new NpgsqlConnection(connectionPostgres))
        {
            connection.Open();

            var transList = new List<string>();
            foreach (var transporteur in transporteurList)
            {
                transList.Add(transporteur.Num.ToString());
            }

            string expediteurQuery = "('" + string.Join("','", transList) + "')";

            // Lecture du transporteur
            string sql = "SELECT * FROM adm.CODEEXPEDITEUR where actif = 1"; 
                //"// AND transnum IN " +  expediteurQuery;
            using (var command = new NpgsqlCommand(sql, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var codifExp = new VOCodificationExpediteur
                        {
                            Expediteur = new VOExpediteurTransport() { Num = Convert.ToInt32(reader["EXPNUM"]) },
                            Codification = Convert.ToString(reader["CODIFICATION"])
                        };

                        codifExpList.Add(codifExp);
                    }
                }
            }
            context.Logger.LogLine("Codification trouvée : ");
            context.Logger.LogLine(codifExpList.ToString());
        }

        // Aucune codification
        if (codifExpList == null || codifExpList.Count <= 0)
        {
            return string.Empty;
        }

        // Recherche de codifications pour l'expediteur
        var codificationsExp = codifExpList.Where(codification => expnum == codification.ExpNum).Select(codification => codification.Codification).ToArray();

        // Aucune codification pour l'expediteur
        if (codificationsExp.Length <= 0)
        {
            return string.Empty;
        }

        return string.Join("-", codificationsExp.ToArray());
    }

    public DateTime GetDateBascule(string connectionPostgres)
    {
        // NuGet Npgsql est le pilote PostgreSQL pour .NET
        // Connexion à la base de données PostgreSQL
        DateTime dateBascule = new DateTime(01, 01, 01);

        using (var connection = new NpgsqlConnection(connectionPostgres))
        {
            connection.Open();

            // Exécution de la requête SQL pour récupérer la date bascule
            string sql = "SELECT parval FROM ADM.PARDIV WHERE parcod = 'DATEPEGGS';";
            using (var command = new NpgsqlCommand(sql, connection))
            {
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    dateBascule = Convert.ToDateTime(result);
                }
            }
        }

        return dateBascule;
    }

    public static void ExportToS3Csv(FileCsv fileCsv, string bucketName, string keyName, ILambdaContext context)
    {
        var csvContent = GenerateCsvContent(fileCsv);
        using (var client = new AmazonS3Client())
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = keyName,
                ContentBody = csvContent
            };

            try
            {
                var response = client.PutObjectAsync(putRequest).GetAwaiter().GetResult();
                context.Logger.LogLine($"Successfully uploaded {keyName} to {bucketName}");
            }
            catch (AmazonS3Exception ex)
            {
                context.Logger.LogLine($"Error uploading {keyName} to {bucketName}: {ex.Message}");
            }
        }
    }

    public static void ExportToS3Xml(string xmlData, string bucketName, string keyName, ILambdaContext context)
    {
        // Créez une instance du client Amazon S3
        var s3Client = new AmazonS3Client();

        // Convertissez la chaîne XML en tableau de bytes
        byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlData);

        // Créez une demande de mise à jour du fichier S3
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = keyName,
            InputStream = new MemoryStream(xmlBytes),
            ContentType = "application/xml"
        };

        try
        {
            // Envoyez la demande pour mettre à jour le fichier S3
            var response = s3Client.PutObjectAsync(putRequest);
            context.Logger.LogLine($"Successfully uploaded {keyName} to {bucketName}");
        }
        catch (AmazonS3Exception ex)
        {
            context.Logger.LogLine($"Error uploading {keyName} to {bucketName}: {ex.Message}");
        }
    }

    public static string GenerateCsvContent(FileCsv fileCsv)
    {
        using (var memoryStream = new MemoryStream())
        using (var streamWriter = new StreamWriter(memoryStream))
        {
            foreach (var row in fileCsv.Header)
            {
                if (row != null)
                {
                    var csvLine = string.Join(",", row);
                    streamWriter.WriteLine(csvLine);
                }
                else
                {
                    streamWriter.WriteLine();
                }
            }

            foreach (var row in fileCsv.Body)
            {
                if (row != null)
                {
                    var csvLine = string.Join(",", row);
                    streamWriter.WriteLine(csvLine);
                }
                else
                {
                    streamWriter.WriteLine();
                }
            }

            streamWriter.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(memoryStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
    #endregion

    public void Exporter(VORequestGenererLimite request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogLine("Lambda 'Exporter Limite' execution started.");
            DateTime DateDebut = request.DateDebut;
            DateTime DateFin = request.DateFin;
            IList<string> Expediteurs = request.Expediteurs as IList<string>;
            IList<string> TypeGeneration = new List<string>() { "XML", "CSV" };

            IList contratsAts = null;
            IList contratsAtr = null;
            IList ListeLimitesStockage = null;
            IList ListeBilans = null;
            string connectionPostgres = Environment.GetEnvironmentVariable("ACCESS_DATABASE");

            // Récupération de tous les Expéditeurs
            if (Expediteurs == null || Expediteurs.Count == 0)
            {
                var expediteursList = new List<string>();
                using (var connection = new NpgsqlConnection(connectionPostgres))
                {
                    connection.Open();

                    // Exécution de la requête SQL pour récupérer les expéditeurs
                    string sql = "SELECT expnum FROM adm.expediteur;";
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string expediteur = reader.GetDouble(0).ToString();
                                expediteursList.Add(expediteur);
                            }
                        }

                        Expediteurs = expediteursList;

                        var nbExpediteur = Expediteurs.Count;
                        context.Logger.LogLine("Expediteurs trouvés : ");
                        context.Logger.LogLine(nbExpediteur.ToString());
                    }
                }
            }

            if (contratsAts == null)
            {
                // Connexion à la base de données PostgreSQL
                using (var connection = new NpgsqlConnection(connectionPostgres))
                {
                    connection.Open();

                    // Exécution de la requête SQL pour récupérer les contrats ATS
                    string sql = "SELECT * FROM adm.contratexp WHERE CTTDTEDEB >= @DateDebut AND CTTDTEFIN <= @DateFin AND CTTTYP = 'ATS';";

                    context.Logger.LogLine(sql.ToString());

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("DateDebut", DateDebut);
                        command.Parameters.AddWithValue("DateFin", DateFin);
                        context.Logger.LogLine(DateDebut.ToString());
                        context.Logger.LogLine(DateFin.ToString());

                        var contratsAtsList = new List<VOContratATS>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var contratAts = new VOContratATS
                                {
                                    ExpediteurTransport = new VOExpediteurTransport() { Num = Convert.ToInt32(reader["EXPNUM"]) },
                                    DateDebut = Convert.ToDateTime(reader["CTTDTEDEB"]),
                                    DateFin = Convert.ToDateTime(reader["CTTDTEFIN"])
                                };
                                contratsAtsList.Add(contratAts);
                            }
                        }

                        contratsAts = contratsAtsList;

                        var nbContratAts = contratsAts.Count;
                        context.Logger.LogLine("Contrat ATS trouvés : ");
                        context.Logger.LogLine(nbContratAts.ToString());
                    }
                }
            }

            // Récupération de tous les contrats ATR des Expéditeurs
            if (contratsAtr == null)
            {
                // Connexion à la base de données PostgreSQL
                using (var connection = new NpgsqlConnection(connectionPostgres))
                {
                    connection.Open();

                    // Exécution de la requête SQL pour récupérer les contrats ATS
                    string sql = "SELECT * FROM adm.contratexp WHERE CTTDTEDEB >= @DateDebut AND CTTDTEFIN <= @DateFin AND CTTTYP = 'TRASP';";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("DateDebut", DateDebut);
                        command.Parameters.AddWithValue("DateFin", DateFin);

                        var contratsAtrList = new List<VOContratATR>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var contratAtr = new VOContratATR
                                {
                                    ExpediteurTransport = new VOExpediteurTransport() { Num = Convert.ToInt32(reader["EXPNUM"]) },
                                    DateDebut = Convert.ToDateTime(reader["CTTDTEDEB"]),
                                    DateFin = Convert.ToDateTime(reader["CTTDTEFIN"])
                                };
                                contratsAtrList.Add(contratAtr);
                            }
                        }

                        contratsAtr = contratsAtrList;

                        var nbContratAtr = contratsAtr.Count;
                        context.Logger.LogLine("Contrat ATR trouvés : ");
                        context.Logger.LogLine(nbContratAtr.ToString());
                    }
                }
            }

            // Pas de contrats
            if (contratsAts.Count == 0)
            {
                context.Logger.LogLine(string.Format("Aucun contrats ATS pour la période {0} à {1}", DateDebut, DateFin));
                return;
            }
            if (contratsAtr.Count == 0)
            {
                context.Logger.LogLine(string.Format("Aucun contrats Trasp pour la période {0} à {1}", DateDebut, DateFin));
                return;
            }

            // Récupération des limites de stockage
            if (ListeLimitesStockage == null)
            {
                // Connexion à la base de données PostgreSQL
                using (var connection = new NpgsqlConnection(connectionPostgres))
                {
                    connection.Open();

                    // Exécution de la requête SQL pour récupérer les limites

                    var expList = new List<string>();
                    foreach (var expediteur in Expediteurs)
                    {
                        expList.Add(expediteur);
                    }

                    string expediteurQuery = "('" + string.Join("','", expList) + "')";

                    string sql = "SELECT * FROM adm.LIMITESTOCKAGE WHERE JOURNEEGAZIERE >= @DateDebut AND JOURNEEGAZIERE <= @DateFin AND ACTIF = 1 AND EXPNUM IN " + expediteurQuery;
                    context.Logger.LogLine(sql);

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("DateDebut", DateDebut);
                        command.Parameters.AddWithValue("DateFin", DateFin.AddDays(2));
                        command.Parameters.AddWithValue("Actif", true);

                        var listeLimitesStockage = new List<VOLimiteStockage>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var limiteStockage = new VOLimiteStockage
                                {
                                    ExpNum = Convert.ToDecimal(reader["EXPNUM"]),
                                    JourneeGaziere = Convert.ToDateTime(reader["JOURNEEGAZIERE"]),
                                    Actif = Convert.ToBoolean(reader["ACTIF"]),
                                    StockFinal = Convert.ToDecimal(reader["STOCKFINAL"]),
                                    StockMin = Convert.ToDecimal(reader["STOCKMIN"]),
                                    StockMax = Convert.ToDecimal(reader["STOCKMAX"]),
                                    StockRef = Convert.ToDecimal(reader["STOCKREF"]),
                                    CltMaxInj = Convert.ToDecimal(reader["CLTMAXINJ"]),
                                    CltMinInj = Convert.ToDecimal(reader["CLTMININJ"]),
                                    CltMaxSout = Convert.ToDecimal(reader["CLTMAXSOUT"]),
                                    CltMinSout = Convert.ToDecimal(reader["CLTMINSOUT"]),
                                    CltMaxInjRed = Convert.ToDecimal(reader["CLTMAXINJRED"]),
                                    CltMinInjRed = Convert.ToDecimal(reader["CLTMININJRED"]),
                                    CltMaxSoutRed = Convert.ToDecimal(reader["CLTMAXSOUTRED"]),
                                    CltMinSoutRed = Convert.ToDecimal(reader["CLTMINSOUTRED"])
                                };
                                listeLimitesStockage.Add(limiteStockage);
                            }
                        }

                        ListeLimitesStockage = listeLimitesStockage;

                        var nbLimiteStockage = ListeLimitesStockage.Count;
                        context.Logger.LogLine("Limite Stockage trouvées : ");
                        context.Logger.LogLine(nbLimiteStockage.ToString());
                    }
                }
            }

            //Aucune limite
            if (ListeLimitesStockage.Count <= 0)
            {
                context.Logger.LogLine(string.Format("Aucune limite de stockage pour les expéditeurs passés en paramètre et pour la période {0} à {1}", DateDebut, DateFin));
                return;
            }

            // Bilans
            if (ListeBilans == null)
            {
                // Connexion à la base de données PostgreSQL
                using (var connection = new NpgsqlConnection(connectionPostgres))
                {
                    connection.Open();

                    // Exécution de la requête SQL pour récupérer les limites
                    string sql = "SELECT * FROM adm.bilanjour WHERE BLJDTE >= @DateDebut AND BLJDTE <= @DateFin;";
                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("DateDebut", DateDebut);
                        command.Parameters.AddWithValue("DateFin", DateFin.AddDays(-2));

                        var listeBilans = new List<VOBilanJournalier>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var bilanJournalier = new VOBilanJournalier
                                {
                                    ExpTransport = new VOExpediteurTransport() { Num = Convert.ToInt32(reader["EXPNUM"]) },
                                    DateBilan = Convert.ToDateTime(reader["BLJDTE"]),
                                    AchatVenteStockage = Convert.ToDecimal(reader["BLJAVS"])
                                };
                                listeBilans.Add(bilanJournalier);
                            }
                        }

                        ListeBilans = listeBilans;

                        var nbBilan = ListeBilans.Count;
                        context.Logger.LogLine("Bilan trouvés : ");
                        context.Logger.LogLine(nbBilan.ToString());
                    }
                }
            }

            IList<VOLimite> voLimites = new List<VOLimite>();

            // ------------------------------------------------------------------------------
            // -- Génération du fichier des limites pour chaque expéditeur et chaque jour. --
            // -- On contrôle si cet expéditeur a un contrat ATS et ATR pour la journée    --
            // ------------------------------------------------------------------------------

            List<int> listeExp = new List<int>();
            foreach (string expediteur in Expediteurs)
            {
                int entier = Convert.ToInt32(expediteur);
                listeExp.Add(entier);
            }

            foreach (int expediteur in listeExp)
            {
                var expe = expediteur;
                // Contrats Ats de l'expéditeur
                var contratsExpAts = contratsAts.Cast<VOContratATS>().Where(x => x.ExpediteurTransport.Num == expe).ToList();

                // Contrats Atr de l'expéditeur
                var contratsExpAtr = contratsAtr.Cast<VOContratATR>().Where(x => x.ExpediteurTransport.Num == expe).ToList();

                if (!contratsExpAtr.Any() || !contratsExpAts.Any())
                {
                    context.Logger.LogLine(string.Format("Aucun contrats Trasp ou ATS pour l'expéditeur {0}", expediteur));
                    continue;
                }

                // De la date de début à la date de fin
                foreach (var date in UtilDate.EachDay(DateDebut, DateFin))
                {
                    var dateLimite = date;
                    var exp = expediteur;

                    var bilanExpJMoins1 = (from bilan in ListeBilans.Cast<VOBilanJournalier>()
                                           where
                                               bilan != null && bilan.ExpTransport != null &&
                                               bilan.ExpTransport.Num == exp &&
                                               bilan.DateBilan.Date == dateLimite.Date.AddDays(-1)
                                           select bilan).FirstOrDefault();

                    var bilanExpJMoins2 = (from bilan in ListeBilans.Cast<VOBilanJournalier>()
                                           where
                                               bilan != null && bilan.ExpTransport != null &&
                                               bilan.ExpTransport.Num == exp &&
                                               bilan.DateBilan.Date == dateLimite.Date.AddDays(-2)
                                           select bilan).FirstOrDefault();

                    // Contrat Ats de l'expéditeur et de la journée
                    var contratExpAts =
                        contratsExpAts.FirstOrDefault(x => x.DateDebut <= dateLimite && x.DateFin >= dateLimite);

                    // Contrat Atr de l'expéditeur et de la journée
                    var contratExpAtr =
                        contratsExpAtr.FirstOrDefault(x => x.DateDebut <= dateLimite && x.DateFin >= dateLimite);

                    // --------------------------------------
                    // | Acheminement | Peg <= date limite   |
                    // --------------------------------------
                    // |    OUI       |     OUI             | -> OUI
                    // --------------------------------------
                    // |    NON       |     OUI             | -> NON
                    // --------------------------------------
                    // |    OUI       |     NON             | -> OUI
                    // --------------------------------------
                    // |    NON       |     NON             | ->
                    // 

                    //if ((GetDateBascule(connectionPostgres) <= dateLimite)) //&& !contratExpAtr.AchmtTransitLivDate(dateLimite))
                    //{
                    //    context.Logger.LogLine(string.Format(
                    //    "Date de limite supérieure à la date du peg GS et contrat trasp sans acheminement pour l'expéditeur {0} pour la date {1}",
                    //    expediteur,
                    //    dateLimite));

                    //    continue;
                    //}

                    if (contratExpAts == null || contratExpAtr == null)
                    {
                        context.Logger.LogLine(string.Format(null, string.Format("Aucun contrats Trasp ou ATS pour l'expéditeur {0} pour la date {1}", expediteur, dateLimite)));
                        continue;
                    }

                    // Limite active pour la journée et l'expéditeur
                    var limitesExpJ = (from limite in ListeLimitesStockage.Cast<VOLimiteStockage>()
                                       where
                                           limite.ExpNum == exp
                                           && limite.JourneeGaziere.Date == dateLimite.Date
                                           && limite.Actif
                                       select limite).ToList();

                    VOLimiteStockage limiteJournalierJ;

                    // Génère un objet métier VOLimite
                    var voLimite = new VOLimite
                    {
                        GasDay = date,
                        ExpNum = expediteur,
                        StockMin = 0,
                        StockMax = 0,
                        CLTInjectionMax = 0,
                        CLTInjectionMin = 0,
                        CLTReducedInjectionMax = 0,
                        CLTReducedInjectionMin = 0,
                        CLTReducedWithDrawalMax = 0,
                        CLTReducedWithDrawalMin = 0,
                        CLTWithDrawalMax = 0,
                        CLTWithDrawalMin = 0,
                        SRUtilise = 0,
                        SFinal = 0,
                        AchatVenteStockage = 0,
                        AuteurMaj = "AWS"
                    };

                    if (limitesExpJ.Count > 0)
                    {
                        limiteJournalierJ = limitesExpJ.FirstOrDefault();
                        voLimite.StockMin = limiteJournalierJ.StockMin;
                        voLimite.StockMax = limiteJournalierJ.StockMax;
                        voLimite.SRUtilise = limiteJournalierJ.StockRef;
                        voLimite.SRUtiliseDate = limiteJournalierJ.JourneeGaziere.AddDays(-1);
                        voLimite.SFinal = limiteJournalierJ.StockFinal;

                        // Définitive
                        if (bilanExpJMoins1 != null && limiteJournalierJ.Type == "D")
                        {
                            voLimite.AchatVenteStockage = bilanExpJMoins1.AchatVenteStockage;
                            voLimite.AchatVenteStockageDate = bilanExpJMoins1.DateBilan;
                            voLimite.SFinalDate = bilanExpJMoins1.DateBilan;
                        }
                        // Provisoire
                        else if (bilanExpJMoins2 != null && limiteJournalierJ.Type == "P")
                        {

                            voLimite.AchatVenteStockage = bilanExpJMoins2.AchatVenteStockage;
                            voLimite.AchatVenteStockageDate = bilanExpJMoins2.DateBilan;
                            voLimite.SFinalDate = bilanExpJMoins2.DateBilan;
                        }

                        if (limiteJournalierJ.CltMaxSout > 0 && limiteJournalierJ.CltMaxInj > 0)
                        {
                            // Si Expéditeur peut soutirer ou injecter
                            // CltMaxSout >0 est une limite de soutirage
                            // CltMaxInj >0 est une limite d’injection.

                            // CLT Withdrawal max = valeur absolue de CltMaxSout
                            voLimite.CLTWithDrawalMax = Math.Abs(limiteJournalierJ.CltMaxSout);

                            // CLT Reduced Withdrawal max = valeur absolue de CltMaxSoutRed
                            voLimite.CLTReducedWithDrawalMax = Math.Abs(limiteJournalierJ.CltMaxSoutRed);

                            // CLT Injection max = valeur absolue de CltMaxInj
                            voLimite.CLTInjectionMax = Math.Abs(limiteJournalierJ.CltMaxInj);

                            // CLT Reduced Injection max = valeur absolue de CltMaxInjRed
                            voLimite.CLTReducedInjectionMax = Math.Abs(limiteJournalierJ.CltMaxInjRed);
                        }
                        else if (limiteJournalierJ.CltMinInj >= 0 && limiteJournalierJ.CltMaxInj > 0)
                        {
                            // Si l’Expéditeur peut injecter seulement
                            // CltMinInj >0 est devenue une limite min d’injection
                            // CltMaxInj >0 est une limite d’injection

                            // CLT Injection min = valeur absolue de CltMinInj
                            voLimite.CLTInjectionMin = Math.Abs(limiteJournalierJ.CltMinInj);

                            // CLT Reduced Injection min = valeur absolue de CltMinInjRed
                            voLimite.CLTReducedInjectionMin = Math.Abs(limiteJournalierJ.CltMinInjRed);

                            // CLT Injection max = valeur absolue de CltMaxInj
                            voLimite.CLTInjectionMax = Math.Abs(limiteJournalierJ.CltMaxInj);

                            // CLT Reduced Injection max = valeur absolue de CltMaxInjRed
                            voLimite.CLTReducedInjectionMax = Math.Abs(limiteJournalierJ.CltMaxInjRed);
                        }
                        else if (limiteJournalierJ.CltMaxSout > 0 && limiteJournalierJ.CltMinSout >= 0)
                        {
                            // Si l’Expéditeur peut soutirer seulement
                            // CltMaxSout >0 est une limite de soutirage
                            // CltMinSout > 0 est devenue une limite min de soutirage. 

                            // CLT Withdrawal max = valeur absolue de CltMaxSout
                            voLimite.CLTWithDrawalMax = Math.Abs(limiteJournalierJ.CltMaxSout);

                            // CLT Reduced Withdrawal max = valeur absolue de CltMaxSoutRed
                            voLimite.CLTReducedWithDrawalMax = Math.Abs(limiteJournalierJ.CltMaxSoutRed);

                            // CLT Withdrawal min = valeur absolue de CltMinSout
                            voLimite.CLTWithDrawalMin = Math.Abs(limiteJournalierJ.CltMinSout);

                            // CLT Reduced Withdrawal min = valeur absolue de CltMinSoutRed
                            voLimite.CLTReducedWithDrawalMin = Math.Abs(limiteJournalierJ.CltMinSoutRed);
                        }
                    }

                    if (limitesExpJ.Count > 0)
                    {
                        voLimites.Add(voLimite);
                    }
                    else
                    {
                        context.Logger.LogLine("Pas de fichier Limite généré pour le " + date + " " +
                            "car pas de limite journalière existant à cette date pour l'expediteur " +
                                                  exp);

                        continue;
                    }

                    var requestExporterLimite = new VORequestExporterLimite
                    {
                        TypeLimites = limitesExpJ.FirstOrDefault() != null ? limitesExpJ.FirstOrDefault().Type : "D",
                        DateDebut = date,
                        DateFin = date,
                        Extensions = (List<string>)TypeGeneration,   
                        Limites = voLimites,
                        Expnum = expediteur
                    };


                    // Appeler la méthode pour exporter les données en CSV vers Amazon S3
                    string bucketName = "buckets3-export";
                    PublierLimites(requestExporterLimite, bucketName, connectionPostgres, context);

                    // nouvelles limites pour l'expediteur et la date
                    voLimites.Clear();

                    context.Logger.LogLine("Lambda 'Exporter Limite' execution completed.");
                }
            }
        }
        catch (Exception e)
        {
            context.Logger.LogLine($"Error occurred: {e.Message}");
        }
    }
}