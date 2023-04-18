function resetFileInput(id) {
    document.getElementById(id).value = "";
}

const loadedScripts = new Set();

function spawn_scripts(scripts) {
    scripts = scripts.filter(s => !loadedScripts.has(s.id));

    if (scripts.length === 0) {
        return;
    }

    var script = document.createElement("script");
    script.src = scripts[0].src;
    script.id = scripts[0].id;
    script.onload = function () {
        console.log(`${scripts[0].src} loaded successfully!`);
        loadedScripts.add(scripts[0].id);
        spawn_scripts(scripts.slice(1));
    };

    document.body.appendChild(script);
}

function spawn_script(src, id) {
    spawn_scripts([{ src: src, id: id }]);
}

function spawn_script(script) {
    spawn_scripts([script]);
}

function despawn_script(id) {
    var script = document.getElementById(id);
    script.parentNode.removeChild(script);
    loadedScripts.delete(id);
}