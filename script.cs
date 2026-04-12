const firebaseConfig = { 
    apiKey: "AIzaSyCDwmQT8q_gL8TxFW8Atdl9JtRo3ywYj98", 
    databaseURL: "https://chat-go12-default-rtdb.firebaseio.com/", 
    storageBucket: "chat-go12.firebasestorage.app", 
    projectId: "chat-go12" 
};
 // --- INITIALIZE FIREBASE ---
firebase.initializeApp(firebaseConfig);
const db = firebase.database();
const st = firebase.storage();

// Set the Node Name in the UI
if(document.getElementById('node-id')) {
    document.getElementById('node-id').innerText = typeof nodeName !== 'undefined' ? nodeName : "VAULT_NODE";
}

// --- 1. AUTHENTICATION ---
function handleAuth() {
    const u = document.getElementById('login-u').value.trim();
    const p = document.getElementById('login-p').value;
    
    // Look for the password in this project's database
    db.ref('admin_config').once('value', s => {
        const data = s.val();
        const storedPass = (data && data.pass) ? data.pass : "yugpatel1309";

        if (u === "Yug Patel" && p === storedPass) {
            document.getElementById('page-login').classList.remove('active');
            document.getElementById('page-files').classList.add('active');
            listenForFiles(); // Start showing the files
        } else {
            alert("ACCESS_DENIED: Invalid Credentials");
        }
    });
}

// --- 2. FAST UPLOAD ENGINE (BASE64 METHOD) ---
async function uploadFile(el) {
    const file = el.files[0];
    if (!file) return;
    
    const status = document.getElementById('status-text');
    status.innerText = "PREPARING...";
    status.style.color = "orange";

    // Step 1: Read file into memory (This makes it instant for small files)
    const reader = new FileReader();
    reader.readAsDataURL(file);
    
    reader.onload = async (e) => {
        status.innerText = "PUSHING TO VAULT...";
        status.style.color = "cyan";
        
        const fileName = Date.now() + "_" + file.name;
        const ref = st.ref('vault/' + fileName);

        try {
            // Step 2: Upload as a Data URL (Bypasses many connection hangs)
            const snapshot = await ref.putString(e.target.result, 'data_url');
            const url = await snapshot.ref.getDownloadURL();

            // Step 3: Save metadata to Database
            await db.ref('vault_meta').push({
                name: file.name,
                sName: fileName,
                url: url,
                size: file.size,
                type: file.type,
                time: Date.now()
            });

            status.innerText = "SUCCESS: VAULTED";
            status.style.color = "#00ff00";
            el.value = ""; // Clear input
        } catch (err) {
            console.error(err);
            // This alert will tell you exactly if it's a Rules or Config issue
            alert("UPLOAD FAILED: " + err.message);
            status.innerText = "FAILED";
            status.style.color = "red";
        }
    };
}

// --- 3. REAL-TIME FILE LISTING ---
function listenForFiles() {
    const list = document.getElementById('file-list');
    
    // This watches the database live
    db.ref('vault_meta').on('value', s => {
        list.innerHTML = "";
        
        if (!s.exists()) {
            list.innerHTML = "<p style='opacity:0.3; font-size:11px;'>Vault is empty.</p>";
            return;
        }

        s.forEach(child => {
            const f = child.val();
            const fileKey = child.key;
            
            list.innerHTML += `
                <div class="admin-card" style="display:flex; justify-content:space-between; align-items:center; margin-bottom:8px; padding:12px;">
                    <div style="overflow:hidden;">
                        <div style="font-size:12px; color:cyan; text-overflow:ellipsis; white-space:nowrap;">${f.name}</div>
                        <div style="font-size:9px; color:gray;">${(f.size/1024).toFixed(1)} KB</div>
                    </div>
                    <div style="display:flex; gap:8px;">
                        <a href="${f.url}" target="_blank" class="nav-btn" style="text-decoration:none; background:#222;">📥</a>
                        <button class="nav-btn" style="background:#400;" onclick="deleteFile('${fileKey}', '${f.sName}')">🗑️</button>
                    </div>
                </div>`;
        });
    });
}

// --- 4. DELETE LOGIC ---
function deleteFile(key, sName) {
    if (!confirm("Permanently delete this file?")) return;
    
    // Delete from Storage
    st.ref('vault/' + sName).delete().then(() => {
        // Delete from Database
        db.ref('vault_meta/' + key).remove();
    }).catch(err => {
        // If storage delete fails (maybe file doesn't exist), still remove from DB
        db.ref('vault_meta/' + key).remove();
    });
}
