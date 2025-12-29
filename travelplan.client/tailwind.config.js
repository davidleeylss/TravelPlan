/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./index.html",
        "./src/**/*.{vue,js,ts,jsx,tsx}",
    ],
    theme: {
        extend: {
            colors: {
                'lake-green': '#66D2CE',
                'lake-dark': '#4FB3AF',
                'soft-gray': '#F5F7FA',
            },
            fontFamily: {
                sans: ['"Noto Sans JP"', 'sans-serif'],
            }
        },
    },
    plugins: [],
}

