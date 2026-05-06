using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace bankova_aplikacia
{
    class Database
    {
        private static FirestoreDb? db;
        private static string projectId = "dominik-39059";

        public static async Task Init()
        {
            try
            {
                string keyPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "firebase-key.json"
                );
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                db = await FirestoreDb.CreateAsync(projectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init chyba: " + ex.Message, "Firebase chyba");
            }
        }

        public static async Task<bool> Registruj(string meno, string gmail, string heslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count > 0)
                    return false;

                Dictionary<string, object> pouzivatel = new Dictionary<string, object>
                {
                    { "Meno", meno },
                    { "Gmail", gmail },
                    { "Heslo", heslo }
                };
                await col.AddAsync(pouzivatel);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registruj chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        public static async Task<bool> Prihlas(string gmail, string heslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail).WhereEqualTo("Heslo", heslo);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Prihlas chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        public static async Task<bool> EmailExistuje(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        public static async Task<bool> ZmenHeslo(string gmail, string noveHeslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count == 0) return false;

                DocumentReference doc = snapshot.Documents[0].Reference;
                await doc.UpdateAsync("Heslo", noveHeslo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        public static async Task UlozHistoriu(string gmail, Dictionary<string, object> investicia)
        {
            try
            {
                CollectionReference col = db!.Collection("Historia");
                await col.AddAsync(investicia);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
            }
        }

        public static async Task<List<Dictionary<string, object>>> NacitajHistoriu(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Historia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                var historia = new List<Dictionary<string, object>>();
                foreach (var doc in snapshot.Documents)
                {
                    historia.Add(doc.ToDictionary());
                }
                return historia;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return new List<Dictionary<string, object>>();
            }
        }

        public static async Task<string> NacitajMeno(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count == 0) return "";

                var doc = snapshot.Documents[0].ToDictionary();
                return doc.ContainsKey("Meno") ? doc["Meno"].ToString()! : "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return "";
            }
        }

        public static async Task<bool> ZmenMeno(string gmail, string noveMeno)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count == 0) return false;

                DocumentReference doc = snapshot.Documents[0].Reference;
                await doc.UpdateAsync("Meno", noveMeno);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        // -- ulozi kupenu poziciu, ak uz existuje pripocita ku existujucej --
        public static async Task UlozPozíciu(string gmail, string symbol, double kusy, double nakupnaCena, double sumaEur)
        {
            try
            {
                CollectionReference col = db!.Collection("Portfolio");

                // -- skontroluj ci uz existuje pozicia s rovnakym symbolom --
                Query query = col.WhereEqualTo("Gmail", gmail).WhereEqualTo("Symbol", symbol);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    // -- uz existuje, pripocitaj kusy a sumu k existujucej pozicii --
                    DocumentReference existujuciDoc = snapshot.Documents[0].Reference;
                    var existujucaData = snapshot.Documents[0].ToDictionary();

                    double doterajsieKusy = existujucaData.ContainsKey("Kusy") ? Convert.ToDouble(existujucaData["Kusy"]) : 0;
                    double doterajsiaSuma = existujucaData.ContainsKey("SumaEur") ? Convert.ToDouble(existujucaData["SumaEur"]) : 0;

                    await existujuciDoc.UpdateAsync(new Dictionary<string, object>
                    {
                        { "Kusy", doterajsieKusy + kusy },
                        { "SumaEur", doterajsiaSuma + sumaEur },
                        { "NakupnaCena", nakupnaCena }
                    });
                }
                else
                {
                    // -- neexistuje, vytvor novu poziciu --
                    Dictionary<string, object> pozicia = new Dictionary<string, object>
                    {
                        { "Gmail", gmail },
                        { "Symbol", symbol },
                        { "Kusy", kusy },
                        { "NakupnaCena", nakupnaCena },
                        { "SumaEur", sumaEur },
                        { "Datum", DateTime.Now.ToString("dd.MM.yyyy HH:mm") }
                    };
                    await col.AddAsync(pozicia);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
            }
        }

        // -- nacita vsetky pozicie portfolia pre daneho uzivatela --
        public static async Task<List<Dictionary<string, object>>> NacitajPortfolio(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Portfolio");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                var portfolio = new List<Dictionary<string, object>>();
                foreach (var doc in snapshot.Documents)
                {
                    // -- pridaj aj id dokumentu aby sme vedeli co mazat pri predaji --
                    var data = doc.ToDictionary();
                    data["DocId"] = doc.Id;
                    portfolio.Add(data);
                }
                return portfolio;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return new List<Dictionary<string, object>>();
            }
        }

        // -- ciastocny predaj pozicie, odpocita kusy a sumu --
        public static async Task<bool> CiastocnyPredaj(string docId, double predavaneKusy, double predavanaSuma)
        {
            try
            {
                DocumentReference doc = db!.Collection("Portfolio").Document(docId);
                DocumentSnapshot snapshot = await doc.GetSnapshotAsync();
                if (!snapshot.Exists) return false;

                var data = snapshot.ToDictionary();
                double aktualneKusy = data.ContainsKey("Kusy") ? Convert.ToDouble(data["Kusy"]) : 0;
                double aktualnaSum = data.ContainsKey("SumaEur") ? Convert.ToDouble(data["SumaEur"]) : 0;

                double zostatokKusy = aktualneKusy - predavaneKusy;
                double zostatokSuma = aktualnaSum - predavanaSuma;

                if (zostatokKusy <= 0)
                {
                    // -- predava vsetko, vymaz poziciu --
                    await doc.DeleteAsync();
                }
                else
                {
                    // -- predava cast, aktualizuj poziciu --
                    await doc.UpdateAsync(new Dictionary<string, object>
                    {
                        { "Kusy", zostatokKusy },
                        { "SumaEur", zostatokSuma }
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        // -- vymaze celu poziciu z portfolia pri predaji --
        public static async Task<bool> PredajPozíciu(string docId)
        {
            try
            {
                DocumentReference doc = db!.Collection("Portfolio").Document(docId);
                await doc.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return false;
            }
        }

        // -- ulozi zostatok na ucte uzivatela --
        public static async Task UlozZostatok(string gmail, double zostatok)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count == 0) return;

                DocumentReference doc = snapshot.Documents[0].Reference;
                await doc.UpdateAsync("Zostatok", zostatok);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
            }
        }

        // -- nacita zostatok z uctu uzivatela --
        public static async Task<double> NacitajZostatok(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                Query query = col.WhereEqualTo("Gmail", gmail);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                if (snapshot.Count == 0) return 0;

                var doc = snapshot.Documents[0].ToDictionary();
                return doc.ContainsKey("Zostatok") ? Convert.ToDouble(doc["Zostatok"]) : 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message, "Firebase chyba");
                return 0;
            }
        }
    }
}