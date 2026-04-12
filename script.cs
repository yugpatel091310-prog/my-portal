const firebaseConfig = { 
    apiKey: "AIzaSyCDwmQT8q_gL8TxFW8Atdl9JtRo3ywYj98", 
    databaseURL: "https://chat-go12-default-rtdb.firebaseio.com/", 
    storageBucket: "chat-go12.firebasestorage.app", 
    projectId: "chat-go12" 
};
const nodeName = "NODE_03";
firebase.initializeApp(firebaseConfig);
const db = firebase.database();
const st = firebase.storage();

document.getElementById('node-id').innerText = nodeName;

function handleAuth() {
    const u = document.getElementById('login-u').value.trim();
    const p = document.getElementById('login-p').value;
    
    // Check this project's own database for password
    db.ref('admin_config').once('value', s => {
        const storedPass = (s.val() && s.val().pass) ? s.val().pass : "yugpatel1309";
        if (u === "Yug Patel" && p === storedPass) {
            document.getElementById('page-login').classList.remove('active');
            document.getElementById('page-files').classList.add('active');
            listenForFiles();
        } else { alert("ACCESS_DENIED"); }
    });
}

function uploadFile(el) {
    const file = el.files[0];
    if (!file) return;
    const status = document.getElementById('status-text');
    status.innerText = "UPLOADING...";

    const fileName = Date.now() + "_" + file.name;
    const ref = st.ref('vault/' + fileName);

    ref.put(file).then(async (snap) => {
        const url = await snap.ref.getDownloadURL();
        await db.ref('vault_meta').push({
            name: file.name,
            sName: fileName,
            url: url,
            size: file.size
        });
        status.innerText = "SUCCESS";
    }).catch(err => {
        alert("ERROR: " + err.message);
        status.innerText = "FAILED";
    });
}

function listenForFiles() {
    db.ref('vault_meta').on('value', s => {
        const list = document.getElementById('file-list');
        list.innerHTML = "";
        s.forEach(child => {
            const f = child.val();
            list.innerHTML += `
                <div class="admin-card" style="display:flex; justify-content:space-between; align-items:center;">
                    <span style="font-size:12px; color:cyan;">${f.name}</span>
                    <a href="${f.url}" target="_blank" class="nav-btn">📥 VIEW</a>
                </div>`;
        });
    });
}

