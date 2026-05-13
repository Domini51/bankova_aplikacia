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
                string keyPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-key.json");
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                db = await FirestoreDb.CreateAsync(projectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init chyba: " + ex.Message);
            }
        }

        public static async Task<bool> Registruj(string meno, string gmail, string heslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count > 0)
                    return false;

                var pouzivatel = new Dictionary<string, object>();
                pouzivatel["Meno"] = meno;
                pouzivatel["Gmail"] = gmail;
                pouzivatel["Heslo"] = heslo;

                await col.AddAsync(pouzivatel);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri registracii: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> Prihlas(string gmail, string heslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).WhereEqualTo("Heslo", heslo).GetSnapshotAsync();
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri prihlaseni: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> EmailExistuje(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> ZmenHeslo(string gmail, string noveHeslo)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count == 0)
                    return false;

                await snapshot.Documents[0].Reference.UpdateAsync("Heslo", noveHeslo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return false;
            }
        }

        public static async Task UlozHistoriu(string gmail, Dictionary<string, object> investicia)
        {
            try
            {
                await db!.Collection("Historia").AddAsync(investicia);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        public static async Task<List<Dictionary<string, object>>> NacitajHistoriu(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Historia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                var historia = new List<Dictionary<string, object>>();
                for (int i = 0; i < snapshot.Documents.Count; i++)
                {
                    historia.Add(snapshot.Documents[i].ToDictionary());
                }
                return historia;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return new List<Dictionary<string, object>>();
            }
        }

        public static async Task<string> NacitajMeno(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count == 0)
                    return "";

                var data = snapshot.Documents[0].ToDictionary();
                if (data.ContainsKey("Meno"))
                    return data["Meno"].ToString()!;
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return "";
            }
        }

        public static async Task<bool> ZmenMeno(string gmail, string noveMeno)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count == 0)
                    return false;

                await snapshot.Documents[0].Reference.UpdateAsync("Meno", noveMeno);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return false;
            }
        }

        public static async Task UlozPozíciu(string gmail, string symbol, double kusy, double nakupnaCena, double sumaEur)
        {
            try
            {
                CollectionReference col = db!.Collection("Portfolio");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).WhereEqualTo("Symbol", symbol).GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    var existData = snapshot.Documents[0].ToDictionary();

                    double stareKusy = 0;
                    double staraSuma = 0;

                    if (existData.ContainsKey("Kusy"))
                        stareKusy = Convert.ToDouble(existData["Kusy"]);
                    if (existData.ContainsKey("SumaEur"))
                        staraSuma = Convert.ToDouble(existData["SumaEur"]);

                    var update = new Dictionary<string, object>();
                    update["Kusy"] = stareKusy + kusy;
                    update["SumaEur"] = staraSuma + sumaEur;
                    update["NakupnaCena"] = nakupnaCena;

                    await snapshot.Documents[0].Reference.UpdateAsync(update);
                }
                else
                {
                    var pozicia = new Dictionary<string, object>();
                    pozicia["Gmail"] = gmail;
                    pozicia["Symbol"] = symbol;
                    pozicia["Kusy"] = kusy;
                    pozicia["NakupnaCena"] = nakupnaCena;
                    pozicia["SumaEur"] = sumaEur;
                    pozicia["Datum"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                    await col.AddAsync(pozicia);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        public static async Task<List<Dictionary<string, object>>> NacitajPortfolio(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Portfolio");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                var portfolio = new List<Dictionary<string, object>>();
                for (int i = 0; i < snapshot.Documents.Count; i++)
                {
                    var data = snapshot.Documents[i].ToDictionary();
                    data["DocId"] = snapshot.Documents[i].Id;
                    portfolio.Add(data);
                }
                return portfolio;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return new List<Dictionary<string, object>>();
            }
        }

        public static async Task<bool> CiastocnyPredaj(string docId, double predavaneKusy, double predavanaSuma)
        {
            try
            {
                DocumentReference doc = db!.Collection("Portfolio").Document(docId);
                DocumentSnapshot snapshot = await doc.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return false;

                var data = snapshot.ToDictionary();

                double aktKusy = 0;
                double aktSuma = 0;

                if (data.ContainsKey("Kusy"))
                    aktKusy = Convert.ToDouble(data["Kusy"]);
                if (data.ContainsKey("SumaEur"))
                    aktSuma = Convert.ToDouble(data["SumaEur"]);

                double novoKusy = aktKusy - predavaneKusy;
                double novoSuma = aktSuma - predavanaSuma;

                if (novoKusy <= 0)
                {
                    await doc.DeleteAsync();
                }
                else
                {
                    var update = new Dictionary<string, object>();
                    update["Kusy"] = novoKusy;
                    update["SumaEur"] = novoSuma;
                    await doc.UpdateAsync(update);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> PredajPozíciu(string docId)
        {
            try
            {
                await db!.Collection("Portfolio").Document(docId).DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return false;
            }
        }

        public static async Task UlozZostatok(string gmail, double zostatok)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count == 0)
                    return;

                await snapshot.Documents[0].Reference.UpdateAsync("Zostatok", zostatok);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        public static async Task<double> NacitajZostatok(string gmail)
        {
            try
            {
                CollectionReference col = db!.Collection("Pouzivatelia");
                QuerySnapshot snapshot = await col.WhereEqualTo("Gmail", gmail).GetSnapshotAsync();

                if (snapshot.Count == 0)
                    return 0;

                var data = snapshot.Documents[0].ToDictionary();
                if (data.ContainsKey("Zostatok"))
                    return Convert.ToDouble(data["Zostatok"]);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
                return 0;
            }
        }
    }
}