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
    }
}