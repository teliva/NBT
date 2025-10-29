let savedFileBlob = null; // store file for later upload
const overlay = document.querySelector('#loading-overlay');

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

    try {
        overlay.classList.remove('d-none');
        const promptText = document.querySelector('#prompt-textarea')?.value || '';
        const formData = new FormData();
        formData.append("Image", savedFileBlob, "uploaded-image.png");
        formData.append("Text", promptText);

        const response = await fetch("www13.kisp.com/nbt/api/generate", {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            throw new Error(`Server responded with status ${response.status}`);
        }

        const imageBlob = await response.blob();
        setRetImage(imageBlob);

    } catch (error) {
        console.error("Error generating image:", error);
    } finally {
        overlay.classList.add('d-none');
    }
};

const setRetImage = (imageBlob) => {
    const img = document.querySelector('#result-image');
    img.src = URL.createObjectURL(imageBlob);
};

init();