using BCrypt.Net;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace bankova_aplikacia
{
    class Database
    {
        static FirestoreDb? db;
        const string PROJECT = "dominik-39059";

        public static async Task Init()
        {
            string keyPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-key.json");

            try
            {
                var credential = GoogleCredential.FromFile(keyPath)
                    .CreateScoped("https://www.googleapis.com/auth/datastore");

                var firestoreClient = await new FirestoreClientBuilder
                {
                    Credential = credential
                }.BuildAsync();

                db = FirestoreDb.Create(PROJECT, firestoreClient);
            }
            catch (Exception ex) { MessageBox.Show("Init chyba: " + ex.Message); }
        }

        static CollectionReference Pouzivatelia() => db!.Collection("Pouzivatelia");
        static CollectionReference Portfolio() => db!.Collection("Portfolio");
        static CollectionReference Historia() => db!.Collection("Historia");

        public static async Task<bool> Registruj(string meno, string gmail, string heslo)
        {
            var existujuci = await Pouzivatelia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            if (existujuci.Count > 0) return false;

            var data = new Dictionary<string, object>
            {
                ["Meno"] = meno,
                ["Gmail"] = gmail,
                ["Heslo"] = BCrypt.Net.BCrypt.HashPassword(heslo)
            };
            await Pouzivatelia().AddAsync(data);
            return true;
        }

        public static async Task<bool> Prihlas(string gmail, string heslo)
        {
            var snap = await Pouzivatelia()
                .WhereEqualTo("Gmail", gmail)
                .GetSnapshotAsync();

            if (snap.Count == 0) return false;

            var doc = snap.Documents[0].ToDictionary();
            string hash = doc["Heslo"].ToString()!;

            // BCrypt hash starts with $2
            if (hash.StartsWith("$2"))
            {
                try { return BCrypt.Net.BCrypt.Verify(heslo, hash); }
                catch { return false; }
            }

            // Legacy SHA256 hash – verify and transparently migrate to BCrypt
            string sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(heslo))).ToLower();
            if (sha256 != hash) return false;

            await snap.Documents[0].Reference.UpdateAsync("Heslo", BCrypt.Net.BCrypt.HashPassword(heslo));
            return true;
        }

        public static async Task<bool> EmailExistuje(string gmail)
        {
            var snap = await Pouzivatelia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            return snap.Count > 0;
        }

        static async Task<DocumentReference?> NajdiPouzivatela(string gmail)
        {
            var snap = await Pouzivatelia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            if (snap.Count == 0) return null;
            return snap.Documents[0].Reference;
        }

        public static async Task<bool> ZmenHeslo(string gmail, string noveHeslo)
        {
            var docRef = await NajdiPouzivatela(gmail);
            if (docRef == null) return false;
            await docRef.UpdateAsync("Heslo", BCrypt.Net.BCrypt.HashPassword(noveHeslo));
            return true;
        }

        public static async Task<bool> ZmenMeno(string gmail, string noveMeno)
        {
            var docRef = await NajdiPouzivatela(gmail);
            if (docRef == null) return false;
            await docRef.UpdateAsync("Meno", noveMeno);
            return true;
        }

        public static async Task<string> NacitajMeno(string gmail)
        {
            var snap = await Pouzivatelia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            if (snap.Count == 0) return "";
            var doc = snap.Documents[0].ToDictionary();
            return doc.ContainsKey("Meno") ? doc["Meno"].ToString()! : "";
        }

        public static async Task<double> NacitajZostatok(string gmail)
        {
            var snap = await Pouzivatelia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            if (snap.Count == 0) return 0;
            var doc = snap.Documents[0].ToDictionary();
            if (!doc.ContainsKey("Zostatok")) return 0;
            return Convert.ToDouble(doc["Zostatok"]);
        }

        public static async Task UlozZostatok(string gmail, double zostatok)
        {
            var docRef = await NajdiPouzivatela(gmail);
            if (docRef != null)
                await docRef.UpdateAsync("Zostatok", zostatok);
        }

        public static async Task UlozHistoriu(string gmail, Dictionary<string, object> investicia)
        {
            await Historia().AddAsync(investicia);
        }

        public static async Task<List<Dictionary<string, object>>> NacitajHistoriu(string gmail)
        {
            var snap = await Historia().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            var vysledok = new List<Dictionary<string, object>>();
            foreach (var doc in snap.Documents)
                vysledok.Add(doc.ToDictionary());
            return vysledok;
        }

        public static async Task UlozPozíciu(string gmail, string symbol, double kusy, double nakupnaCena, double sumaEur)
        {
            var snap = await Portfolio()
                .WhereEqualTo("Gmail", gmail)
                .WhereEqualTo("Symbol", symbol)
                .GetSnapshotAsync();

            if (snap.Count > 0)
            {
                var existData = snap.Documents[0].ToDictionary();
                double stareKusy = existData.ContainsKey("Kusy") ? Convert.ToDouble(existData["Kusy"]) : 0;
                double staraSuma = existData.ContainsKey("SumaEur") ? Convert.ToDouble(existData["SumaEur"]) : 0;

                await snap.Documents[0].Reference.UpdateAsync(new Dictionary<string, object>
                {
                    ["Kusy"] = stareKusy + kusy,
                    ["SumaEur"] = staraSuma + sumaEur,
                    ["NakupnaCena"] = nakupnaCena
                });
            }
            else
            {
                await Portfolio().AddAsync(new Dictionary<string, object>
                {
                    ["Gmail"] = gmail,
                    ["Symbol"] = symbol,
                    ["Kusy"] = kusy,
                    ["NakupnaCena"] = nakupnaCena,
                    ["SumaEur"] = sumaEur,
                    ["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });
            }
        }

        public static async Task<List<Dictionary<string, object>>> NacitajPortfolio(string gmail)
        {
            var snap = await Portfolio().WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
            var vysledok = new List<Dictionary<string, object>>();

            foreach (var doc in snap.Documents)
            {
                var data = doc.ToDictionary();
                data["DocId"] = doc.Id;
                vysledok.Add(data);
            }
            return vysledok;
        }

        public static async Task<bool> CiastocnyPredaj(string docId, double predavaneKusy, double predavanaSuma)
        {
            var doc = Portfolio().Document(docId);
            var snap = await doc.GetSnapshotAsync();
            if (!snap.Exists) return false;

            var data = snap.ToDictionary();
            double aktKusy = data.ContainsKey("Kusy") ? Convert.ToDouble(data["Kusy"]) : 0;
            double aktSuma = data.ContainsKey("SumaEur") ? Convert.ToDouble(data["SumaEur"]) : 0;

            double novoKusy = aktKusy - predavaneKusy;

            if (novoKusy <= 0)
                await doc.DeleteAsync();
            else
                await doc.UpdateAsync(new Dictionary<string, object>
                {
                    ["Kusy"] = novoKusy,
                    ["SumaEur"] = aktSuma - predavanaSuma
                });

            return true;
        }

        public static async Task<bool> PredajPozíciu(string docId)
        {
            await Portfolio().Document(docId).DeleteAsync();
            return true;
        }
    }
}