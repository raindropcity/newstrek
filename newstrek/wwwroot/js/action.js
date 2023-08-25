const sections = document.querySelectorAll('.section')
const secBtns = document.querySelector('.controlls')
const secBtn = document.querySelectorAll('.control')
const allSections = document.querySelector('.main-content')
const themeBtn = document.querySelector('.theme-btn')
// const LanBtn = document.querySelector('.language-btn')

function PageTransitions() {
    // Click btn to add the className: active-btn
    // 一開始home按鈕預設有className → active-btn (按鈕會變綠色，代表現正觀看的頁面)，當監聽到某按鈕被點擊時，將原本有 active-btn 者的 active-btn 替換成空字串，並用this.classList.add('active-btn') 為被點擊的按鈕 className 加上 active-btn。  因為需要這種一消一長的效果，所以不採用toggle。
    for (let i = 0; i < secBtn.length; i++) {
        secBtn[i].addEventListener('click', function EventHandler() {
            let currentBtn = document.querySelector('.active-btn')
            currentBtn.classList = currentBtn.className.replace('active-btn', '') // '' is empty string
            this.classList.add('active-btn') // 有this的話不能用箭頭函式
            console.log(123)
        })
    }
    // Click btn to add the className: active
    allSections.addEventListener('click', function EventHandler(event) {
        const id = event.target.dataset.id // 抓data-id屬性值
        if (id) {
            // 針對section(整個視窗的div)，邏輯同上
            sections.forEach((section) => {
                section.classList.remove('active')
            })
            const element = document.getElementById(id) //透過data-id比對該元素的id屬性
            element.classList.add('active')
        }
    })
}

// function changeThemeColor() {
//   // 轉換主題顏色(使用toggle)
//   themeBtn.addEventListener('click', function EventHandler() {
//     const HTMLbody = document.body // 抓HTML的body元素出來
//     HTMLbody.classList.toggle('light-mode') // 點擊時，為此元素的className加上.light-mode，再次點擊時，移除.light-mode
//   })
// }

// const EnLanguage = document.querySelectorAll('.En-language')
// const ChLanguage = document.querySelectorAll('.Ch-language')
// function changeLanguage() {
//     LanBtn.addEventListener('click', function EventHandler() {
//         EnLanguage.forEach((each) => {
//             each.classList.toggle('language-hidden')
//         })
//         ChLanguage.forEach((each) => {
//             each.classList.toggle('language-show-up')
//         })
//         // EnLanguage.classList.toggle('language-hidden')
//         // ChLanguage.classList.toggle('language-show-up')
//     })
// }

// 按首頁相片二側的箭頭圖案可以換相片
const leftArrow = document.querySelector('.left-arrow')
const rightArrow = document.querySelector('.right-arrow')
const headerPhotos = document.querySelectorAll('.left-header .vocabulary')

function switchHeaderPhoto() {
    if (leftArrow) {
        leftArrow.addEventListener('click', function EventHandler() {
            const headerPhotoArray = []
            headerPhotos.forEach((photo) => {
                headerPhotoArray.push(photo)
            })

            const showUpPhoto = document.querySelector('.show-up')
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

            const showUpPhoto = document.querySelector('.show-up')
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

// PageTransitions()
// changeThemeColor()
switchHeaderPhoto()
// changeLanguage()