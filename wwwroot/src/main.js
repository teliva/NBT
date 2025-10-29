let savedFileBlob = null; // store file for later upload

document.querySelector("#generate-btn").addEventListener("click", (ev) => generate(ev));

const init = () => {
    document.querySelector("#drop-zone").addEventListener("dragover", (ev) => {
        ev.preventDefault();
    });

    document.querySelector("#drop-zone").addEventListener("drop", (ev) => onImageDropped(ev));
}

const onImageDropped = (ev) => {
    ev.preventDefault();

    const processFile = (file) => {
        const reader = new FileReader();
        
        reader.onload = (event) => {
            const result = event.target?.result;
            if (result instanceof ArrayBuffer) {
                savedFileBlob = new Blob([result], { type: file.type || "image/png" });

                const url = URL.createObjectURL(savedFileBlob);
                document.querySelector('#kits-image').src = url;
            } else {
                console.error("Unexpected result type:", typeof result);
            }
        };
        reader.onerror = (error) => {
            console.error("Error reading file:", error);
        };
        reader.readAsArrayBuffer(file);
    };

    if (ev.dataTransfer.items) {
        // Use DataTransferItemList interface to access the file(s)
        [...ev.dataTransfer.items].forEach((item, i) => {
            if (item.kind === "file") {
                const file = item.getAsFile();
                processFile(file);
            }
        });
    } else {
        // Use DataTransfer interface to access the file(s)
        [...ev.dataTransfer.files].forEach((file, i) => {
            processFile(file);
        });
    }
}

const generate = async () => {
    if (!savedFileBlob) return;

    const formData = new FormData();
    formData.append("Image", savedFileBlob, "uploaded-image.png");
    formData.append("Text", document.querySelector('#prompt-textarea').value);

    const response = await fetch("/api/generate", {
        method: "POST",
        body: formData
    });

    const result = await response.json();
    console.log(result);
}


init();


