import sqlite3

conn = sqlite3.connect('DiplomovaPrace/metering.db')
cursor = conn.cursor()
cursor.execute("DELETE FROM AppUsers WHERE Email NOT IN ('matej.klibr@tul.cz', 'admin@example.com', 'viewer@example.com')")
print(f"Deleted {cursor.rowcount} test users")
conn.commit()

cursor.execute("SELECT Email FROM AppUsers")
print("Remaining users:", cursor.fetchall())
conn.close()
