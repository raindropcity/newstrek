﻿document.addEventListener('DOMContentLoaded', () => {
    // Get the query string
    const queryString = window.location.search;
    // Create a URLSearchParams object to handle the query string
    const params = new URLSearchParams(queryString);
    // Get the value of the "url" parameter
    const num = params.get("num");

    const container = document.querySelector(".blog")
    console.log(num)
    if (num) {
        fetch(`/ElasticSearch/search-news-by-num?num=${num}`)
            .then(response => response.json())
            .then(response => {
                console.log(response)

                container.innerHTML = `
                    <h3>${response[0].title}</h3>
                    <div class="blog-text">
                        ${response[0].article}
                    </div>
                `
                // 等API response的資料加入HTML後，在".blog-text"加上監聽器，監聽"mouseup"事件
                attachLookupListeners()
            })
            .catch(error => console.error('error in GET request to search news by category', error))
    }
})

function attachLookupListeners() {
    const article = document.querySelector('.blog-text')

    article.addEventListener('mouseup', (e) => {
        const selectedText = window.getSelection().toString().trim()
        console.log(selectedText)
        if (selectedText !== '') {
            showLookupOption(e.clientX, e.clientY, selectedText)
        }
    })
}

function showLookupOption(x, y, selectedText) {
    // Create a lookup option (e.g., a tooltip)
    const lookupOption = document.createElement('div');
    lookupOption.className = 'lookup-option';
    lookupOption.textContent = 'Look up';

    // Position the option near the selected text
    lookupOption.style.left = x + 'px';
    lookupOption.style.top = y + 'px';

    // Add a click event to perform the lookup action
    lookupOption.addEventListener('click', () => {
        performLookup(selectedText);
        lookupOption.remove();
    });

    // Append the option to the document body
    document.body.appendChild(lookupOption);
}

function performLookup(vocabulary) {
    console.log(`performLookUp: ${vocabulary}`)

    const defCard_1 = document.querySelector(".ref-1 .blog-text")
    const defCard_2 = document.querySelector(".ref-2 .blog-text")
    const defModal = document.querySelector(".def-modal")
    const x = document.querySelector(".def-modal h5")

    // When "x" been clicked, set the css of def-modal from "show-up" to "hidden"
    x.addEventListener('click', () => {
        defModal.classList.remove('show-up')
        defModal.classList.add('hidden')
    })

    fetch(`/Dictionary/look-up-words-crawler-Merriam-Webster?word=${vocabulary}`)
        .then(data => {
            data.text().then((content) => {
                if (content) defCard_1.innerHTML = content
                else if (!content) defCard_1.innerHTML = `\"${vocabulary}\" is not included in Merriam Webster Dictionary.`
            })
        })
        .catch(error => {
            console.error('Error in GET request to crwal vocabulary in Merriam Webster', error);
        })

    fetch(`/Dictionary/look-up-words-crawler-Longman?word=${vocabulary}`)
        .then(data => {
            data.text().then((content) => {
                if (content) defCard_2.innerHTML = content
                else if (!content) defCard_2.innerHTML = `\"${vocabulary}\" is not included in Longman Dictionary.`
            })
        })
        .catch(error => {
            console.error('Error in GET request to crwal vocabulary in Longman', error);
        })

    defModal.classList.remove('hidden')
    defModal.classList.add('show-up')
}

// 換字典
const leftArrow = document.querySelector('.left-arrow')
const rightArrow = document.querySelector('.right-arrow')
const headerPhotos = document.querySelectorAll('.def-modal .vocabulary')

function switchHeaderPhoto() {
    if (leftArrow) {
        leftArrow.addEventListener('click', function EventHandler() {
            const headerPhotoArray = []
            headerPhotos.forEach((photo) => {
                headerPhotoArray.push(photo)
            })

            const showUpPhoto = document.querySelector('.def-modal .show-up')
            const index = headerPhotoArray.indexOf(showUpPhoto)

            if ((index - 1) < 0) {
                showUpPhoto.classList.remove('show-up')
                showUpPhoto.classList.add('hidden')
                headerPhotos[headerPhotos.length - 1].classList.add('show-up')
                headerPhotos[headerPhotos.length - 1].classList.remove('hidden')
            }
            else if ((index - 1) >= 0) {
                showUpPhoto.classList.add('hidden')
                showUpPhoto.classList.remove('show-up')
                headerPhotos[index - 1].classList.add('show-up')
                headerPhotos[index - 1].classList.remove('hidden')
            }
        })
    }

    if (rightArrow) {
        rightArrow.addEventListener('click', function EventHandler() {
            const headerPhotoArray = []
            headerPhotos.forEach((photo) => {
                headerPhotoArray.push(photo)
            })

            const showUpPhoto = document.querySelector('.def-modal .show-up')
            const index = headerPhotoArray.indexOf(showUpPhoto)

            if ((index + 1) >= headerPhotos.length) {
                showUpPhoto.classList.remove('show-up')
                showUpPhoto.classList.add('hidden')
                headerPhotos[0].classList.add('show-up')
                headerPhotos[0].classList.remove('hidden')
            }
            else if ((index + 1) < headerPhotos.length) {
                showUpPhoto.classList.add('hidden')
                showUpPhoto.classList.remove('show-up')
                headerPhotos[index + 1].classList.add('show-up')
                headerPhotos[index + 1].classList.remove('hidden')
            }
        })
    }
}

switchHeaderPhoto()